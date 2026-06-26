using Erp.Application.Tasks.Contracts;
using Erp.Shared.Results;

namespace Erp.Application.Tasks;

public interface ITaskNoteService
{
    Task<Result<IReadOnlyList<TaskNoteDto>>> ListAsync(Guid taskId, CancellationToken ct = default);
    Task<Result<Guid>> AddAsync(Guid taskId, CreateNoteRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid taskId, Guid noteId, UpdateNoteRequest request, CancellationToken ct = default);
    Task<Result> RemoveAsync(Guid taskId, Guid noteId, CancellationToken ct = default);
}

public interface ITaskDocumentService
{
    Task<Result<IReadOnlyList<TaskDocumentDto>>> ListAsync(Guid taskId, CancellationToken ct = default);
    Task<Result<Guid>> AddAsync(Guid taskId, CreateDocumentRequest request, CancellationToken ct = default);
    Task<Result> RemoveAsync(Guid taskId, Guid documentId, CancellationToken ct = default);
}
