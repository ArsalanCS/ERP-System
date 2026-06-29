using Erp.Application.Abstractions;
using Erp.Domain.Assets;
using Erp.Domain.Authorization;
using Erp.Domain.Tasks;
using Erp.Domain.Identity;
using Erp.Domain.Mailing;
using Erp.Domain.Tenancy;
using Erp.Domain.Statuses;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Seeding;

/// <summary>Seeds the global permission catalog and per-workspace system roles.</summary>
public interface IIdentitySeeder
{
    /// <summary>Upserts the global permission catalog (idempotent, no tenant scope).</summary>
    Task SeedPermissionCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Upserts the global event-type and asset-type catalogues (idempotent, no tenant scope).</summary>
    Task SeedEventAssetCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a workspace has the standard "Workspace Owner" system role granting
    /// every permission at workspace scope. Requires a tenant scope to be active.
    /// </summary>
    Task<Role> EnsureWorkspaceOwnerRoleAsync(long workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Development convenience: creates a demo workspace + owner user (assigned the
    /// Workspace Owner role) when the slug does not already exist. Idempotent.
    /// Never call this in production — owners are provisioned through onboarding.
    /// </summary>
    Task EnsureDemoWorkspaceAsync(string slug, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds a workspace's default TASK_STATUS (New/In Progress/Done/Cancelled) and
    /// TASK_PRIORITY (Low/Medium/High/Critical) status types if it has none.
    /// </summary>
    Task SeedDefaultStatusesAsync(long workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts the GLOBAL mail-template catalogue (workspace_id NULL = shared defaults) for all task
    /// notification codes. Idempotent — inserts only missing global codes. No tenant scope (Mail doc §4).
    /// </summary>
    Task SeedMailTemplatesCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings existing workspaces up to date at startup: re-grants the owner role any
    /// newly added catalog permissions and seeds default statuses. Idempotent.
    /// </summary>
    Task SyncExistingWorkspacesAsync(CancellationToken cancellationToken = default);
}

public sealed class IdentitySeeder(ErpDbContext context, ITenantContext tenant, IPasswordHasher passwordHasher) : IIdentitySeeder
{
    public const string WorkspaceOwnerRoleCode = "workspace-owner";

    public async Task SeedPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        var existing = await context.Permissions.Select(p => p.Code).ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.Ordinal);

        var added = false;
        foreach (var def in PermissionCatalog.All.Where(d => !existingSet.Contains(d.Code)))
        {
            context.Permissions.Add(new Permission(def.Code, def.Module, def.Resource, def.Action, def.IsHighRisk));
            added = true;
        }

        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SeedEventAssetCatalogAsync(CancellationToken cancellationToken = default)
    {
        // Full Event/Asset catalogues (architecture §6/§10). Task Management is the only
        // active event type this phase; the rest are seeded so future modules resolve by code.
        (string Code, string Name)[] eventTypes =
        [
            (EventTypeCodes.TaskManagement, "Task Management"),
            (EventTypeCodes.Issue, "Issue"),
            (EventTypeCodes.Maintenance, "Maintenance"),
            (EventTypeCodes.Approval, "Approval"),
            (EventTypeCodes.SalesActivity, "Sales Activity"),
        ];
        (string Code, string Name)[] assetTypes =
        [
            (AssetTypeCodes.Note, "Note"),
            (AssetTypeCodes.Document, "Document"),
            (AssetTypeCodes.Customer, "Customer"),
            (AssetTypeCodes.Supplier, "Supplier"),
            (AssetTypeCodes.Vehicle, "Vehicle"),
            (AssetTypeCodes.Invoice, "Invoice"),
            (AssetTypeCodes.Resource, "Resource"),
        ];

        var existingEvent = (await context.EventTypes.Select(e => e.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var existingAsset = (await context.AssetTypes.Select(a => a.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var added = false;
        foreach (var (code, name) in eventTypes.Where(t => !existingEvent.Contains(t.Code)))
        {
            context.EventTypes.Add(new EventType(code, name));
            added = true;
        }
        foreach (var (code, name) in assetTypes.Where(t => !existingAsset.Contains(t.Code)))
        {
            context.AssetTypes.Add(new AssetType(code, name));
            added = true;
        }
        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Role> EnsureWorkspaceOwnerRoleAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        using var _ = tenant.BeginScope(workspaceId, [], isPlatformAdmin: true);

        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Code == WorkspaceOwnerRoleCode, cancellationToken);

        if (role is null)
        {
            role = new Role(workspaceId, "Workspace Owner", WorkspaceOwnerRoleCode, RoleType.System,
                "Full administrative access to the workspace.");
            context.Roles.Add(role);
            await context.SaveChangesAsync(cancellationToken);
        }

        var permissionIds = await context.Permissions.Select(p => p.Id).ToListAsync(cancellationToken);
        var alreadyGranted = await context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
        var grantedSet = alreadyGranted.ToHashSet();

        // System role: grant everything at workspace scope (bypassing the public guard).
        foreach (var permissionId in permissionIds.Where(id => !grantedSet.Contains(id)))
        {
            context.RolePermissions.Add(new RolePermission(workspaceId, role.Id, permissionId, DataScope.Workspace));
        }

        await context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task EnsureDemoWorkspaceAsync(string slug, string email, string password, CancellationToken cancellationToken = default)
    {
        // Already seeded? Bail (idempotent). Platform-admin scope bypasses RLS for the insert.
        using (tenant.BeginScope(null, [], isPlatformAdmin: true))
        {
            if (await context.Workspaces.AnyAsync(w => w.Slug == slug, cancellationToken))
            {
                return;
            }

            var workspace = new Workspace($"WS {slug}", slug, "en", "Asia/Riyadh", "SAR");
            workspace.Activate();
            context.Workspaces.Add(workspace);

            var user = new User(workspace.Id, email, "Demo", "Owner");
            user.SetPasswordHash(passwordHasher.Hash(password));
            user.Activate();
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            var role = await EnsureWorkspaceOwnerRoleAsync(workspace.Id, cancellationToken);

            context.UserRoles.Add(new UserRole(workspace.Id, user.Id, role.Id));
            await context.SaveChangesAsync(cancellationToken);
        }

        var demoWorkspaceId = (await GetWorkspaceIdAsync(slug, cancellationToken))!.Value;
        await SeedDefaultStatusesAsync(demoWorkspaceId, cancellationToken);
    }

    public async Task SeedDefaultStatusesAsync(long workspaceId, CancellationToken cancellationToken = default)
    {
        using var _ = tenant.BeginScope(workspaceId, [], isPlatformAdmin: true);

        if (await context.StatusTypes.AnyAsync(t => t.WorkspaceId == workspaceId, cancellationToken))
        {
            return;
        }

        // TASK_STATUS workflow.
        var statusType = new StatusType(workspaceId, StatusTypeCodes.TaskStatus, "Task Status");
        context.StatusTypes.Add(statusType);
        void AddStatus(string code, string name, int sort, bool initial, bool closed, string color)
            => context.Statuses.Add(new Status(workspaceId, statusType.Id, code, name, sort, initial, closed, color));
        AddStatus("NEW", "New", 0, initial: true, closed: false, "#64748b");
        AddStatus("IN_PROGRESS", "In Progress", 1, initial: false, closed: false, "#2563eb");
        AddStatus("DONE", "Done", 2, initial: false, closed: true, "#16a34a");
        AddStatus("CANCELLED", "Cancelled", 3, initial: false, closed: true, "#dc2626");

        // TASK_PRIORITY values.
        var priorityType = new StatusType(workspaceId, StatusTypeCodes.TaskPriority, "Task Priority");
        context.StatusTypes.Add(priorityType);
        void AddPriority(string code, string name, int sort, string color)
            => context.Statuses.Add(new Status(workspaceId, priorityType.Id, code, name, sort, isInitial: false, isClosed: false, color));
        AddPriority("LOW", "Low", 0, "#64748b");
        AddPriority("MEDIUM", "Medium", 1, "#2563eb");
        AddPriority("HIGH", "High", 2, "#d97706");
        AddPriority("CRITICAL", "Critical", 3, "#dc2626");

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedMailTemplatesCatalogAsync(CancellationToken cancellationToken = default)
    {
        // Global defaults (workspace_id NULL). No tenant scope; platform-admin bypasses RLS for the read/insert.
        using var _ = tenant.BeginScope(null, [], isPlatformAdmin: true);

        var existing = (await context.MailTemplates
            .Where(t => t.WorkspaceId == null)
            .Select(t => t.Code).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        var added = false;
        foreach (var (code, name, subject, body) in DefaultMailTemplates.Where(t => !existing.Contains(t.code)))
        {
            context.MailTemplates.Add(new MailTemplate(workspaceId: null, code, name, subject, body, bodyTextTemplate: null));
            added = true;
        }
        if (added)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SyncExistingWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        List<long> ids;
        using (tenant.BeginScope(null, [], isPlatformAdmin: true))
        {
            ids = await context.Workspaces.Select(w => w.Id).ToListAsync(cancellationToken);
        }

        foreach (var id in ids)
        {
            await EnsureWorkspaceOwnerRoleAsync(id, cancellationToken); // grants any new catalog permissions
            await SeedDefaultStatusesAsync(id, cancellationToken);
        }
    }

    private static readonly (string code, string name, string subject, string body)[] DefaultMailTemplates =
    [
        (MailTemplateCodes.TaskCreated, "Task Created",
            "New task {{TaskRef}}: {{TaskTitle}}",
            "<p>A new task <strong>{{TaskRef}} — {{TaskTitle}}</strong> was created by {{Actor}}.</p>"),
        (MailTemplateCodes.TaskAssigned, "Task Assigned",
            "You've been assigned {{TaskRef}}: {{TaskTitle}}",
            "<p>{{Actor}} assigned <strong>{{TaskRef}} — {{TaskTitle}}</strong> to you. Priority: {{Priority}}. Due: {{DueDate}}.</p>"),
        (MailTemplateCodes.TaskOpened, "Task Opened",
            "{{TaskRef}} has started",
            "<p>Work has started on <strong>{{TaskRef}} — {{TaskTitle}}</strong> (status {{Status}}).</p>"),
        (MailTemplateCodes.TaskStatusChanged, "Task Status Changed",
            "{{TaskRef}} is now {{Status}}",
            "<p>The status of <strong>{{TaskRef}} — {{TaskTitle}}</strong> was changed from {{OldStatus}} to <strong>{{Status}}</strong> by {{Actor}}.</p>"),
        (MailTemplateCodes.TaskCompleted, "Task Completed",
            "{{TaskRef}} completed",
            "<p><strong>{{TaskRef}} — {{TaskTitle}}</strong> was completed ({{Status}}) by {{Actor}}.</p>"),
        (MailTemplateCodes.DailyReportSubmitted, "Daily Report Submitted",
            "Daily report on {{TaskRef}}",
            "<p>{{Actor}} filed a daily report on <strong>{{TaskRef}} — {{TaskTitle}}</strong> for {{Date}}.</p><p>{{DailyReportDescription}}</p>"),
        (MailTemplateCodes.DailyReportStatusChanged, "Daily Report Status Changed",
            "{{TaskRef}}: report + status now {{Status}}",
            "<p>{{Actor}} filed a daily report on <strong>{{TaskRef}} — {{TaskTitle}}</strong> for {{Date}} and changed status from {{OldStatus}} to <strong>{{Status}}</strong>.</p><p>{{DailyReportDescription}}</p>"),
    ];

    private async Task<long?> GetWorkspaceIdAsync(string slug, CancellationToken cancellationToken)
    {
        using var _ = tenant.BeginScope(null, [], isPlatformAdmin: true);
        return await context.Workspaces.Where(w => w.Slug == slug).Select(w => (long?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
