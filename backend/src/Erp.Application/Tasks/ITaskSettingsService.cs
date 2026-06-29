using Erp.Application.Tasks.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Tasks;

/// <summary>
/// Task Management settings: manage the workspace's TASK_STATUS workflow and TASK_PRIORITY
/// values (create / update / reorder / delete). Requires task.workflow.manage.
/// </summary>
public interface ITaskSettingsService
{
    Task<Result<IReadOnlyList<StatusDto>>> ListAsync(string statusTypeCode, CancellationToken ct = default);
    Task<Result<Guid>> CreateStatusAsync(CreateStatusRequest request, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task<Result> ReorderAsync(ReorderStatusesRequest request, CancellationToken ct = default);
    Task<Result> DeleteStatusAsync(Guid id, CancellationToken ct = default);

    Task<Result<TaskSettingsDto>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result> UpdateSettingsAsync(UpdateTaskSettingsRequest request, CancellationToken ct = default);
}
