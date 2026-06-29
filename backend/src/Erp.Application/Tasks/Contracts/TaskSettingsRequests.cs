namespace Erp.Application.Tasks.Contracts;

/// <summary>Creates a status value under a status type (TASK_STATUS or TASK_PRIORITY).</summary>
public sealed record CreateStatusRequest(
    string StatusTypeCode,
    string Name,
    string? Color,
    bool IsClosed,
    bool IsInitial);

public sealed record UpdateStatusRequest(
    string Name,
    string? Color,
    bool IsClosed,
    bool IsInitial,
    bool IsActive);

/// <summary>Sets the display order of statuses within a type to match the given id sequence.</summary>
public sealed record ReorderStatusesRequest(string StatusTypeCode, IReadOnlyList<Guid> OrderedIds);

/// <summary>Workspace-level Task Management config (daily-report rules, notifications, dashboard defaults).</summary>
public sealed record TaskSettingsDto(
    bool DailyReportRequired,
    bool AllowStatusChangeFromReport,
    bool RequireActualTime,
    bool RequireEstimatedTime,
    bool AllowMultipleReportsPerDay,
    bool NotifyOnTaskCreated,
    bool NotifyOnTaskAssigned,
    bool NotifyOnStatusChange,
    bool NotifyOnDailyReport,
    int DashboardDefaultRangeDays);

public sealed record UpdateTaskSettingsRequest(
    bool DailyReportRequired,
    bool AllowStatusChangeFromReport,
    bool RequireActualTime,
    bool RequireEstimatedTime,
    bool AllowMultipleReportsPerDay,
    bool NotifyOnTaskCreated,
    bool NotifyOnTaskAssigned,
    bool NotifyOnStatusChange,
    bool NotifyOnDailyReport,
    int DashboardDefaultRangeDays);
