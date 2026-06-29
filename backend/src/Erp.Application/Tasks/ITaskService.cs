using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Tasks;

/// <summary>
/// Task Management on the Event/Asset architecture. A task is an Event(TASK_MANAGEMENT)
/// plus a TaskEvent; workflow status lives in EventStatus history; priority is a Status
/// under TASK_PRIORITY; notes/documents are Assets linked via EventAsset. Reads respect
/// the caller's task.view DataScope; writes are audited and logged.
/// </summary>
public interface ITaskService
{
    Task<Result<PagedResult<TaskListItemDto>>> ListAsync(TaskListQuery query, CancellationToken ct = default);
    Task<Result<TaskDetailsDto>> GetAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<MyTasksGroups>> GetMyTasksAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskListItemDto>>> ListSubtasksAsync(Guid eventId, CancellationToken ct = default);

    Task<Result<TaskDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<TaskReportDto>> GetReportAsync(TaskListQuery query, CancellationToken ct = default);
    Task<Result<PagedResult<TaskDailyReportRowDto>>> GetDailyReportsReportAsync(TaskDailyReportQuery query, CancellationToken ct = default);

    Task<Result<CreateTaskResult>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<Result<CreateTaskResult>> CreateSubtaskAsync(Guid parentEventId, CreateTaskRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid eventId, UpdateTaskRequest request, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(Guid eventId, ChangeStatusRequest request, CancellationToken ct = default);
    Task<Result> AssignAsync(Guid eventId, AssignTaskRequest request, CancellationToken ct = default);
    Task<Result> SetPriorityAsync(Guid eventId, SetPriorityRequest request, CancellationToken ct = default);
    Task<Result> ArchiveAsync(Guid eventId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskActivityDto>>> GetActivityAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskAuditDto>>> GetAuditAsync(Guid eventId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<StatusDto>>> ListStatusesAsync(string statusTypeCode, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskNoteDto>>> ListNotesAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<Guid>> AddNoteAsync(Guid eventId, CreateNoteRequest request, CancellationToken ct = default);
    Task<Result> UpdateNoteAsync(Guid eventId, Guid noteId, UpdateNoteRequest request, CancellationToken ct = default);
    Task<Result> RemoveNoteAsync(Guid eventId, Guid noteId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDocumentDto>>> ListDocumentsAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<Guid>> AddDocumentAsync(Guid eventId, CreateDocumentRequest request, CancellationToken ct = default);
    Task<Result> RemoveDocumentAsync(Guid eventId, Guid documentId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDependencyDto>>> ListDependenciesAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<Guid>> AddDependencyAsync(Guid eventId, CreateDependencyRequest request, CancellationToken ct = default);
    Task<Result> RemoveDependencyAsync(Guid eventId, Guid dependencyId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDailyReportDto>>> ListDailyReportsAsync(Guid eventId, CancellationToken ct = default);
    Task<Result<Guid>> AddDailyReportAsync(Guid eventId, CreateDailyReportRequest request, CancellationToken ct = default);
    Task<Result> UpdateDailyReportAsync(Guid eventId, Guid reportId, UpdateDailyReportRequest request, CancellationToken ct = default);
    Task<Result> RemoveDailyReportAsync(Guid eventId, Guid reportId, CancellationToken ct = default);
}
