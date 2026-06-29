namespace Erp.Application.Tasks.Contracts;

/// <summary>A status/priority breakdown bucket (id may be null for "no status/priority").</summary>
public sealed record TaskBucketDto(long? Id, string? Name, string? Color, int Count);

/// <summary>Open-task load per assignee (null id = unassigned).</summary>
public sealed record TaskAssigneeLoadDto(long? AssigneeId, string? AssigneeName, int Open, int Overdue);

/// <summary>A single day's created/completed counts for the activity trend.</summary>
public sealed record TaskTrendPointDto(DateOnly Date, int Created, int Completed);

/// <summary>A recent activity feed entry across the visible task set.</summary>
public sealed record TaskRecentActivityDto(
    long Id,
    long EventId,
    string ReferenceNo,
    string Message,
    long? ActorId,
    string? ActorName,
    DateTimeOffset OccurredAt);

/// <summary>A schedule bar for the dashboard mini-gantt (open tasks that have start/due dates).</summary>
public sealed record TaskGanttItemDto(
    long EventId,
    string ReferenceNo,
    string Title,
    DateTimeOffset? StartAt,
    DateTimeOffset? DueAt,
    int CompletionPercent,
    string? StatusColor,
    bool IsClosed);

/// <summary>
/// Task Management dashboard: headline KPIs, status/priority breakdowns, assignee load,
/// and a recent activity trend. Scoped to what the caller may see (task.view DataScope).
/// </summary>
public sealed record TaskDashboardDto(
    int Total,
    int Open,
    int InProgress,
    int Overdue,
    int DueToday,
    int DueThisWeek,
    int HighPriority,
    int Completed,
    int Unassigned,
    int CompletedLast7Days,
    int ReportsToday,
    int AvgCompletionPercent,
    decimal EstimatedTotal,
    decimal ActualTotal,
    IReadOnlyList<TaskBucketDto> ByStatus,
    IReadOnlyList<TaskBucketDto> ByPriority,
    IReadOnlyList<TaskAssigneeLoadDto> ByAssignee,
    IReadOnlyList<TaskTrendPointDto> Trend,
    IReadOnlyList<TaskRecentActivityDto> RecentActivity,
    IReadOnlyList<TaskGanttItemDto> Gantt);

/// <summary>
/// Aggregate summary for a (filtered) report set: counts and time totals plus the same
/// breakdowns. Pairs with the paginated task list for the tabular rows.
/// </summary>
public sealed record TaskReportDto(
    int Total,
    int Open,
    int Completed,
    int Overdue,
    decimal EstimatedTotal,
    decimal ActualTotal,
    IReadOnlyList<TaskBucketDto> ByStatus,
    IReadOnlyList<TaskBucketDto> ByPriority,
    IReadOnlyList<TaskAssigneeLoadDto> ByAssignee);
