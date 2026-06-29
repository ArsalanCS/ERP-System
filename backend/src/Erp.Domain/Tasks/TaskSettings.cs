using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// Per-workspace Task Management settings (Dashboard/Settings doc §7): daily-report rules, mail
/// notification toggles, and dashboard defaults. One row per workspace; sensible defaults apply when
/// no row exists yet.
/// </summary>
public sealed class TaskSettings : TenantEntity
{
    private TaskSettings() { } // EF

    public TaskSettings(long workspaceId)
    {
        AssignWorkspace(workspaceId);
        AllowStatusChangeFromReport = true;
        NotifyOnTaskCreated = true;
        NotifyOnTaskAssigned = true;
        NotifyOnStatusChange = true;
        NotifyOnDailyReport = true;
        DashboardDefaultRangeDays = 14;
    }

    // Daily report rules (doc §7.4)
    public bool DailyReportRequired { get; private set; }
    public bool AllowStatusChangeFromReport { get; private set; }
    public bool RequireActualTime { get; private set; }
    public bool RequireEstimatedTime { get; private set; }
    public bool AllowMultipleReportsPerDay { get; private set; }

    // Notification toggles (doc §7.5)
    public bool NotifyOnTaskCreated { get; private set; }
    public bool NotifyOnTaskAssigned { get; private set; }
    public bool NotifyOnStatusChange { get; private set; }
    public bool NotifyOnDailyReport { get; private set; }

    // Dashboard defaults (doc §7.1)
    public int DashboardDefaultRangeDays { get; private set; }

    public void Update(
        bool dailyReportRequired, bool allowStatusChangeFromReport, bool requireActualTime, bool requireEstimatedTime,
        bool allowMultipleReportsPerDay, bool notifyOnTaskCreated, bool notifyOnTaskAssigned, bool notifyOnStatusChange,
        bool notifyOnDailyReport, int dashboardDefaultRangeDays)
    {
        DailyReportRequired = dailyReportRequired;
        AllowStatusChangeFromReport = allowStatusChangeFromReport;
        RequireActualTime = requireActualTime;
        RequireEstimatedTime = requireEstimatedTime;
        AllowMultipleReportsPerDay = allowMultipleReportsPerDay;
        NotifyOnTaskCreated = notifyOnTaskCreated;
        NotifyOnTaskAssigned = notifyOnTaskAssigned;
        NotifyOnStatusChange = notifyOnStatusChange;
        NotifyOnDailyReport = notifyOnDailyReport;
        DashboardDefaultRangeDays = dashboardDefaultRangeDays < 1 ? 14 : dashboardDefaultRangeDays;
    }
}
