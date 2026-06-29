using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Erp.Infrastructure.Authorization;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Authorization;

/// <summary>
/// Verifies the permission evaluation order (spec §5.5) against real Postgres:
/// role grants apply, and an explicit user deny overrides a role allow (§5.2).
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PermissionResolverTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Role_grant_is_effective_and_user_deny_override_wins()
    {
        var code = $"test.view.{Guid.NewGuid():N}";
        long workspaceId, userId, permissionId;

        // Seed catalog + role + assignment as platform admin.
        await using (var ctx = fixture.CreateContext(out _, platformAdmin: true))
        {
            var ws = new Workspace($"WS {code}", $"ws-{Guid.NewGuid():N}"[..16], "en", "Asia/Riyadh", "SAR");
            ctx.Workspaces.Add(ws);
            var permission = new Permission(code, "Test", "Thing", "View");
            ctx.Permissions.Add(permission);
            var user = new User(ws.Id, $"{Guid.NewGuid():N}@x.test", "T", "U");
            ctx.Users.Add(user);
            var role = new Role(ws.Id, "Viewer", $"viewer-{Guid.NewGuid():N}"[..16], RoleType.Custom);
            ctx.Roles.Add(role);
            await ctx.SaveChangesAsync();

            ctx.RolePermissions.Add(new RolePermission(ws.Id, role.Id, permission.Id, DataScope.Workspace));
            ctx.UserRoles.Add(new UserRole(ws.Id, user.Id, role.Id));
            await ctx.SaveChangesAsync();

            workspaceId = ws.Id;
            userId = user.Id;
            permissionId = permission.Id;
        }

        // Resolved within the workspace scope: the role grant is effective.
        await using (var ctx = fixture.CreateContext(out _, workspaceId: workspaceId))
        {
            var effective = await new PermissionResolver(ctx).ResolveAsync(userId);
            Assert.True(effective.Can(code));
        }

        // Add an explicit deny override.
        await using (var ctx = fixture.CreateContext(out _, workspaceId: workspaceId))
        {
            ctx.UserPermissions.Add(new UserPermission(workspaceId, userId, permissionId, PermissionEffect.Deny));
            await ctx.SaveChangesAsync();
        }

        // Deny wins — the action is no longer effective.
        await using (var ctx = fixture.CreateContext(out _, workspaceId: workspaceId))
        {
            var effective = await new PermissionResolver(ctx).ResolveAsync(userId);
            Assert.False(effective.Can(code));
        }
    }
}
