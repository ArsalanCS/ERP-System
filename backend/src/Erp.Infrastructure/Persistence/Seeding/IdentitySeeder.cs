using Erp.Application.Abstractions;
using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tasks;
using Erp.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Erp.Domain.Tasks.TaskStatus; // disambiguate from System.Threading.Tasks.TaskStatus

namespace Erp.Infrastructure.Persistence.Seeding;

/// <summary>Seeds the global permission catalog and per-workspace system roles.</summary>
public interface IIdentitySeeder
{
    /// <summary>Upserts the global permission catalog (idempotent, no tenant scope).</summary>
    Task SeedPermissionCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a workspace has the standard "Workspace Owner" system role granting
    /// every permission at workspace scope. Requires a tenant scope to be active.
    /// </summary>
    Task<Role> EnsureWorkspaceOwnerRoleAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Development convenience: creates a demo workspace + owner user (assigned the
    /// Workspace Owner role) when the slug does not already exist. Idempotent.
    /// Never call this in production — owners are provisioned through onboarding.
    /// </summary>
    Task EnsureDemoWorkspaceAsync(string slug, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Seeds a workspace's "Default Task Workflow" (New/In Progress/Done/Cancelled) if it has none.</summary>
    Task SeedDefaultTaskWorkflowAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Brings existing workspaces up to date at startup: re-grants the owner role any
    /// newly added catalog permissions and seeds a default task workflow. Idempotent.
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

    public async Task<Role> EnsureWorkspaceOwnerRoleAsync(Guid workspaceId, CancellationToken cancellationToken = default)
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
        using (tenant.BeginScope(Guid.Empty, [], isPlatformAdmin: true))
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

        await SeedDefaultTaskWorkflowAsync(
            (await GetWorkspaceIdAsync(slug, cancellationToken))!.Value, cancellationToken);
    }

    public async Task SeedDefaultTaskWorkflowAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        using var _ = tenant.BeginScope(workspaceId, [], isPlatformAdmin: true);

        if (await context.TaskStatusTypes.AnyAsync(t => t.WorkspaceId == workspaceId, cancellationToken))
        {
            return;
        }

        var type = new TaskStatusType(workspaceId, "Default Task Workflow");
        type.SetDefault(true);
        context.TaskStatusTypes.Add(type);

        void AddStatus(string name, TaskStatusCategory category, int sort, bool initial, bool final)
        {
            var status = new TaskStatus(workspaceId, type.Id, name, category);
            status.Update(name, category, null, sort);
            status.SetInitial(initial);
            status.SetFinal(final);
            context.TaskStatuses.Add(status);
        }

        AddStatus("New", TaskStatusCategory.Open, 0, initial: true, final: false);
        AddStatus("In Progress", TaskStatusCategory.InProgress, 1, initial: false, final: false);
        AddStatus("Done", TaskStatusCategory.Completed, 2, initial: false, final: true);
        AddStatus("Cancelled", TaskStatusCategory.Cancelled, 3, initial: false, final: true);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncExistingWorkspacesAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> ids;
        using (tenant.BeginScope(Guid.Empty, [], isPlatformAdmin: true))
        {
            ids = await context.Workspaces.Select(w => w.Id).ToListAsync(cancellationToken);
        }

        foreach (var id in ids)
        {
            await EnsureWorkspaceOwnerRoleAsync(id, cancellationToken); // grants any new catalog permissions
            await SeedDefaultTaskWorkflowAsync(id, cancellationToken);
        }
    }

    private async Task<Guid?> GetWorkspaceIdAsync(string slug, CancellationToken cancellationToken)
    {
        using var _ = tenant.BeginScope(Guid.Empty, [], isPlatformAdmin: true);
        return await context.Workspaces.Where(w => w.Slug == slug).Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
