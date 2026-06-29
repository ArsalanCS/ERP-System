using Erp.Application.Common;

namespace Erp.Application.Tasks.Contracts;

/// <summary>Filters for the task list (Event/Asset Task Management).</summary>
public sealed record TaskListQuery : ListQuery
{
    public long? StatusId { get; init; }
    public long? PriorityStatusId { get; init; }
    public long? AssigneeId { get; init; }
    public bool? Overdue { get; init; }
    public bool? ClosedOnly { get; init; }
    public long? ParentEventId { get; init; }
}

/// <summary>Filters for the workspace-wide daily-reports report (date range + people/status).</summary>
public sealed record TaskDailyReportQuery : ListQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public long? AuthorId { get; init; }
    public long? StatusId { get; init; }
}
