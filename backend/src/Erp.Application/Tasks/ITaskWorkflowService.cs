using Erp.Application.Tasks.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Tasks;

/// <summary>Workspace-admin management of task status workflows (Task Model spec §4).</summary>
public interface ITaskWorkflowService
{
    Task<Result<IReadOnlyList<TaskWorkflowDto>>> GetWorkflowsAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateStatusTypeAsync(CreateStatusTypeRequest request, CancellationToken ct = default);
    Task<Result> UpdateStatusTypeAsync(Guid id, UpdateStatusTypeRequest request, CancellationToken ct = default);
    Task<Result> ArchiveStatusTypeAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> CreateStatusAsync(CreateStatusRequest request, CancellationToken ct = default);
    Task<Result> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default);
    Task<Result> ArchiveStatusAsync(Guid id, CancellationToken ct = default);
}
