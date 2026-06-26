using Erp.Application.Abstractions;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Auditing;
using Erp.Domain.Identity;
using Erp.Domain.Tasks;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Tasks;

/// <summary>
/// Task documents (Task Model spec §5). Reference-based for now (name + type +
/// link); binary upload arrives with shared file storage. Visibility enforced via
/// <see cref="ITaskService.GetAsync"/>.
/// </summary>
public sealed class TaskDocumentService(
    IRepository<TaskDocument> documents,
    IRepository<User> users,
    ITaskService tasks,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskDocumentService
{
    public async Task<Result<IReadOnlyList<TaskDocumentDto>>> ListAsync(Guid taskId, CancellationToken ct = default)
    {
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure<IReadOnlyList<TaskDocumentDto>>(TaskErrors.NotFound("Task"));
        var items = await (
            from d in documents.Query()
            where d.TaskId == taskId
            join u in users.Query() on d.UploadedBy equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby d.CreatedAt descending
            select new TaskDocumentDto(d.Id, d.FileName, d.FileType, d.Url, d.Note, d.UploadedBy, u != null ? u.DisplayName : null, d.CreatedAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskDocumentDto>>(items);
    }

    public async Task<Result<Guid>> AddAsync(Guid taskId, CreateDocumentRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<Guid>(TaskErrors.NoScope());
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure<Guid>(TaskErrors.NotFound("Task"));

        var doc = new TaskDocument(ws, taskId, request.FileName, request.FileType, request.Url, request.Note, currentUser.UserId);
        documents.Add(doc);
        await audit.LogAsync(Entry(AuditActions.Create, doc.Id, ws), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(doc.Id);
    }

    public async Task<Result> RemoveAsync(Guid taskId, Guid documentId, CancellationToken ct = default)
    {
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure(TaskErrors.NotFound("Task"));
        var doc = await documents.Query().FirstOrDefaultAsync(d => d.Id == documentId && d.TaskId == taskId, ct);
        if (doc is null) return Result.Failure(TaskErrors.NotFound("Document"));
        doc.SoftDelete(currentUser.UserId, clock.UtcNow);
        await audit.LogAsync(Entry(AuditActions.Delete, doc.Id, doc.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static AuditEntry Entry(string action, Guid id, Guid ws) => new()
    {
        Action = action, Module = "Tasks", ResourceType = "TaskDocument", ResourceId = id.ToString(), WorkspaceId = ws,
    };
}
