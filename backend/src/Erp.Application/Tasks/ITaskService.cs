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
    Task<Result<TaskDetailsDto>> GetAsync(long eventId, CancellationToken ct = default);
    Task<Result<MyTasksGroups>> GetMyTasksAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskListItemDto>>> ListSubtasksAsync(long eventId, CancellationToken ct = default);

    Task<Result<TaskDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<TaskReportDto>> GetReportAsync(TaskListQuery query, CancellationToken ct = default);
    Task<Result<PagedResult<TaskDailyReportRowDto>>> GetDailyReportsReportAsync(TaskDailyReportQuery query, CancellationToken ct = default);

    Task<Result<CreateTaskResult>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<Result<CreateTaskResult>> CreateSubtaskAsync(long parentEventId, CreateTaskRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(long eventId, UpdateTaskRequest request, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(long eventId, ChangeStatusRequest request, CancellationToken ct = default);
    Task<Result> AssignAsync(long eventId, AssignTaskRequest request, CancellationToken ct = default);
    Task<Result> SetPriorityAsync(long eventId, SetPriorityRequest request, CancellationToken ct = default);
    Task<Result> ArchiveAsync(long eventId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskActivityDto>>> GetActivityAsync(long eventId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TaskAuditDto>>> GetAuditAsync(long eventId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<StatusDto>>> ListStatusesAsync(string statusTypeCode, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskNoteDto>>> ListNotesAsync(long eventId, CancellationToken ct = default);
    Task<Result<long>> AddNoteAsync(long eventId, CreateNoteRequest request, CancellationToken ct = default);
    Task<Result> UpdateNoteAsync(long eventId, long noteId, UpdateNoteRequest request, CancellationToken ct = default);
    Task<Result> RemoveNoteAsync(long eventId, long noteId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDocumentDto>>> ListDocumentsAsync(long eventId, CancellationToken ct = default);
    Task<Result<long>> AddDocumentAsync(long eventId, CreateDocumentRequest request, CancellationToken ct = default);
    Task<Result> RemoveDocumentAsync(long eventId, long documentId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDependencyDto>>> ListDependenciesAsync(long eventId, CancellationToken ct = default);
    Task<Result<long>> AddDependencyAsync(long eventId, CreateDependencyRequest request, CancellationToken ct = default);
    Task<Result> RemoveDependencyAsync(long eventId, long dependencyId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskDailyReportDto>>> ListDailyReportsAsync(long eventId, CancellationToken ct = default);
    Task<Result<long>> AddDailyReportAsync(long eventId, CreateDailyReportRequest request, CancellationToken ct = default);
    Task<Result> UpdateDailyReportAsync(long eventId, long reportId, UpdateDailyReportRequest request, CancellationToken ct = default);
    Task<Result> RemoveDailyReportAsync(long eventId, long reportId, CancellationToken ct = default);
}
