namespace Erp.Infrastructure.Persistence.ReadModels;

// Row Models for the bpm.fn_task_* DB functions. Keyless (HasNoKey, ToView(null)),
// populated via FromSqlRaw, then mapped explicitly to the Application DTOs in
// TaskReadRepository (company standard: DB Function -> Row Model -> DTO).

public sealed class TaskSummaryRow
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Overdue { get; set; }
    public int DueToday { get; set; }
    public int DueThisWeek { get; set; }
    public int HighPriority { get; set; }
    public int Completed { get; set; }
    public int Unassigned { get; set; }
    public int CompletedLast7 { get; set; }
    public int ReportsToday { get; set; }
    public int AvgCompletion { get; set; }
    public decimal EstimatedTotal { get; set; }
    public decimal ActualTotal { get; set; }
}

public sealed class TaskBucketRow
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public int Count { get; set; }
}

public sealed class TaskAssigneeLoadRow
{
    public long? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public int Open { get; set; }
    public int Overdue { get; set; }
}

public sealed class TaskTrendRow
{
    public DateOnly Day { get; set; }
    public int Created { get; set; }
    public int Completed { get; set; }
}

public sealed class TaskRecentActivityRow
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string ReferenceNo { get; set; } = "";
    public string Message { get; set; } = "";
    public long? ActorId { get; set; }
    public string? ActorName { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class TaskGanttRow
{
    public long EventId { get; set; }
    public string ReferenceNo { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public int CompletionPercent { get; set; }
    public string? StatusColor { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class TaskListItemRow
{
    public long EventId { get; set; }
    public string ReferenceNo { get; set; } = "";
    public string Title { get; set; } = "";
    public long? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StatusColor { get; set; }
    public bool StatusIsClosed { get; set; }
    public long? PriorityStatusId { get; set; }
    public string? PriorityName { get; set; }
    public string? PriorityColor { get; set; }
    public long? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public bool IsOverdue { get; set; }
    public int CompletionPercent { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int TotalCount { get; set; }
}

public sealed class TaskDailyReportRow
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string ReferenceNo { get; set; } = "";
    public string TaskTitle { get; set; } = "";
    public DateOnly ReportDate { get; set; }
    public string Description { get; set; } = "";
    public decimal? EstimatedTime { get; set; }
    public decimal? ActualTime { get; set; }
    public decimal? RemainingTime { get; set; }
    public long? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? StatusColor { get; set; }
    public long? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long Total { get; set; }
}
