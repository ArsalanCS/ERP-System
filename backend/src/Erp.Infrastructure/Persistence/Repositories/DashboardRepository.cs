using Erp.Application.Dashboard;
using Erp.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Counts for the dashboard. Every query is auto-scoped to the active workspace
/// by the global query filter, so no cross-tenant aggregates leak (spec §3.3).
/// </summary>
public sealed class DashboardRepository(ErpDbContext context) : IDashboardRepository
{
    public async Task<DashboardSummary> GetSummaryAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var totalUsers = await context.Users.CountAsync(ct);
        var activeUsers = await context.Users.CountAsync(u => u.Status == UserStatus.Active, ct);
        var suspendedUsers = await context.Users.CountAsync(u => u.Status == UserStatus.Suspended, ct);
        var pendingInvites = await context.Users.CountAsync(u => u.Status == UserStatus.PendingInvitation, ct);
        var organizations = await context.Organizations.CountAsync(ct);
        var clusters = await context.Clusters.CountAsync(ct);
        var roles = await context.Roles.CountAsync(ct);
        var activeSessions = await context.RefreshTokens.CountAsync(t => t.RevokedAt == null && t.ExpiresAt > now, ct);

        return new DashboardSummary(totalUsers, activeUsers, suspendedUsers, pendingInvites,
            organizations, clusters, roles, activeSessions);
    }
}
