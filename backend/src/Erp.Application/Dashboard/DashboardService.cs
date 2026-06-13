using Erp.Application.Abstractions;

namespace Erp.Application.Dashboard;

/// <summary>Scope-aware KPI counts for the admin dashboard (Identity spec §3).</summary>
public sealed record DashboardSummary(
    int TotalUsers, int ActiveUsers, int SuspendedUsers, int PendingInvitations,
    int Organizations, int Clusters, int Roles, int ActiveSessions);

public interface IDashboardRepository
{
    Task<DashboardSummary> GetSummaryAsync(DateTimeOffset now, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);
}

public sealed class DashboardService(IDashboardRepository repo, IClock clock) : IDashboardService
{
    public Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default) => repo.GetSummaryAsync(clock.UtcNow, ct);
}
