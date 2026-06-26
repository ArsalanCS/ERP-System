using Erp.Application.Common;
using Erp.Domain.Tasks;

namespace Erp.Application.Tasks.Contracts;

/// <summary>List/filter inputs for tasks (Refactor Guide §5.3). Bound from the query string.</summary>
public sealed record TaskListQuery : ListQuery
{
    public Guid? StatusId { get; init; }
    public Guid? AssigneeId { get; init; }
    public TaskPriority? Priority { get; init; }
    public TaskStatusCategory? Category { get; init; }
    public bool? Overdue { get; init; }
    public Guid? ParentTaskId { get; init; }
}
