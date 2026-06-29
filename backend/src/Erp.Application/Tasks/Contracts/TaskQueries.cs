using Erp.Application.Common;

namespace Erp.Application.Tasks.Contracts;

/// <summary>Filters for the task list (Event/Asset Task Management).</summary>
public sealed record TaskListQuery : ListQuery
{
    public Guid? StatusId { get; init; }
    public Guid? PriorityStatusId { get; init; }
    public Guid? AssigneeId { get; init; }
    public bool? Overdue { get; init; }
    public bool? ClosedOnly { get; init; }
    public Guid? ParentEventId { get; init; }
}

/// <summary>Filters for the workspace-wide daily-reports report (date range + people/status).</summary>
public sealed record TaskDailyReportQuery : ListQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? AuthorId { get; init; }
    public Guid? StatusId { get; init; }
}
