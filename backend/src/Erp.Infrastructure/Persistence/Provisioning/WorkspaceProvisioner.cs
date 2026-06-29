using Erp.Application.Abstractions;
using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Erp.Infrastructure.Persistence.Seeding;

namespace Erp.Infrastructure.Persistence.Provisioning;

/// <summary>
/// Provisions a new workspace + owner during self-service signup. Mirrors the
/// demo seeder (<see cref="IdentitySeeder.EnsureDemoWorkspaceAsync"/>) but is
/// parameterized for real registrations. Runs under a platform-admin tenant
/// scope so the workspace/user inserts bypass RLS (there is no caller scope yet).
/// </summary>
public sealed class WorkspaceProvisioner(ErpDbContext context, ITenantContext tenant, IIdentitySeeder seeder)
    : IWorkspaceProvisioner
{
    public async Task<WorkspaceProvisionResult> ProvisionAsync(
        WorkspaceProvisionRequest request, CancellationToken cancellationToken = default)
    {
        using (tenant.BeginScope(null, [], isPlatformAdmin: true))
        {
            var workspace = new Workspace(
                request.WorkspaceName, request.Slug, request.DefaultLanguage, request.TimeZone, request.BaseCurrency);
            if (request.ActivateImmediately)
            {
                workspace.Activate();
            }
            // else: stays Trial — AllowsLogin is true, but the owner is PendingInvitation
            // and cannot log in until they verify their email.
            context.Workspaces.Add(workspace);

            var user = new User(workspace.Id, request.Email, request.FirstName, request.LastName);
            user.SetPasswordHash(request.PasswordHash);
            if (request.ActivateImmediately)
            {
                user.Activate();
            }
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            // Ensure the global catalog exists, then grant the owner every permission
            // at workspace scope via the standard system role.
            await seeder.SeedPermissionCatalogAsync(cancellationToken);
            var role = await seeder.EnsureWorkspaceOwnerRoleAsync(workspace.Id, cancellationToken);

            context.UserRoles.Add(new UserRole(workspace.Id, user.Id, role.Id));
            await context.SaveChangesAsync(cancellationToken);

            // New workspaces start with default task statuses + priorities so tasks work immediately.
            // Mail templates are global defaults (seeded once at startup); workspaces may add overrides.
            await seeder.SeedDefaultStatusesAsync(workspace.Id, cancellationToken);

            return new WorkspaceProvisionResult(workspace.Id, user.Id);
        }
    }
}
