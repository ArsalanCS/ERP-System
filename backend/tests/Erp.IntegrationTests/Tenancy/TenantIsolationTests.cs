using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Erp.IntegrationTests.Tenancy;

/// <summary>
/// Proves tenant isolation in BOTH layers against real PostgreSQL (CLAUDE.md
/// §4.1, CONVENTIONS testing): the EF Core global query filter AND the RLS
/// policy. A cross-tenant read/write is a critical security bug.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TenantIsolationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Query_is_scoped_to_the_active_workspace_by_the_ef_filter()
    {
        var (workspaceA, workspaceB, _, _) = await SeedTwoWorkspacesAsync();

        await using var ctx = fixture.CreateContext(out _, workspaceId: workspaceA);
        var emails = await ctx.Users.Select(u => u.WorkspaceId).Distinct().ToListAsync();

        Assert.All(emails, ws => Assert.Equal(workspaceA, ws));
        Assert.DoesNotContain(workspaceB, emails);
    }

    [Fact]
    public async Task Rls_blocks_cross_tenant_read_even_when_ef_filter_is_ignored()
    {
        var (workspaceA, _, _, userBId) = await SeedTwoWorkspacesAsync();

        await using var ctx = fixture.CreateContext(out _, workspaceId: workspaceA);

        // Bypass the application-layer filter on purpose — the DB-layer RLS policy
        // must still hide workspace B's row.
        var leaked = await ctx.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userBId);

        Assert.Null(leaked);
    }

    [Fact]
    public async Task Rls_with_check_blocks_writing_a_row_into_another_workspace()
    {
        var (workspaceA, workspaceB, _, _) = await SeedTwoWorkspacesAsync();

        await using var ctx = fixture.CreateContext(out _, workspaceId: workspaceA);

        // Attempt to insert a user that belongs to workspace B while scoped to A.
        ctx.Users.Add(new User(workspaceB, $"intruder-{Guid.NewGuid():n}@x.test", "In", "Truder"));

        // The audit interceptor's tenant guard and/or the RLS WITH CHECK reject it.
        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Platform_admin_can_read_across_workspaces()
    {
        var (workspaceA, workspaceB, _, _) = await SeedTwoWorkspacesAsync();

        await using var ctx = fixture.CreateContext(out _, platformAdmin: true);
        var workspaceIds = await ctx.Users.Select(u => u.WorkspaceId).Distinct().ToListAsync();

        Assert.Contains(workspaceA, workspaceIds);
        Assert.Contains(workspaceB, workspaceIds);
    }

    private async Task<(Guid wsA, Guid wsB, Guid userAId, Guid userBId)> SeedTwoWorkspacesAsync()
    {
        await using var ctx = fixture.CreateContext(out _, platformAdmin: true);

        var suffix = Guid.NewGuid().ToString("n")[..8];
        var wsA = new Workspace($"WS A {suffix}", $"ws-a-{suffix}", "en", "Asia/Riyadh", "SAR");
        var wsB = new Workspace($"WS B {suffix}", $"ws-b-{suffix}", "en", "Asia/Riyadh", "SAR");
        ctx.Workspaces.AddRange(wsA, wsB);

        var userA = new User(wsA.Id, $"a-{suffix}@x.test", "Alice", "A");
        var userB = new User(wsB.Id, $"b-{suffix}@x.test", "Bob", "B");
        ctx.Users.AddRange(userA, userB);

        await ctx.SaveChangesAsync();
        return (wsA.Id, wsB.Id, userA.Id, userB.Id);
    }
}
