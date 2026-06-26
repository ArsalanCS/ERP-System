using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Tasks;

public interface ITaskService
{
    Task<Result<PagedResult<TaskListItemDto>>> ListAsync(TaskListQuery query, CancellationToken ct = default);
    Task<Result<TaskDetailsDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<CreateTaskResult>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct = default);
    Task<Result> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken ct = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskActivityDto>>> GetActivityAsync(Guid id, CancellationToken ct = default);
    Task<Result<MyTasksGroups>> GetMyTasksAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskAuditDto>>> GetAuditAsync(Guid id, CancellationToken ct = default);

    // Subtasks (child task events)
    Task<Result<IReadOnlyList<TaskListItemDto>>> ListSubtasksAsync(Guid id, CancellationToken ct = default);
    Task<Result<CreateTaskResult>> CreateSubtaskAsync(Guid parentId, CreateTaskRequest request, CancellationToken ct = default);

    // Checklist
    Task<Result<IReadOnlyList<ChecklistItemDto>>> ListChecklistAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> AddChecklistItemAsync(Guid id, CreateChecklistItemRequest request, CancellationToken ct = default);
    Task<Result> UpdateChecklistItemAsync(Guid id, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default);
    Task<Result> RemoveChecklistItemAsync(Guid id, Guid itemId, CancellationToken ct = default);

    // Dependencies
    Task<Result<IReadOnlyList<TaskDependencyDto>>> ListDependenciesAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> AddDependencyAsync(Guid id, CreateDependencyRequest request, CancellationToken ct = default);
    Task<Result> RemoveDependencyAsync(Guid id, Guid dependencyId, CancellationToken ct = default);

    // Relations (links to other business records)
    Task<Result<IReadOnlyList<TaskRelationDto>>> ListRelationsAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> AddRelationAsync(Guid id, CreateRelationRequest request, CancellationToken ct = default);
    Task<Result> RemoveRelationAsync(Guid id, Guid relationId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskRelationDto>>> RefreshRelationsAsync(Guid id, CancellationToken ct = default);
}
