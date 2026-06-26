using Erp.Application.Abstractions;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Auditing;
using Erp.Domain.Identity;
using Erp.Domain.Tasks;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Tasks;

/// <summary>
/// Task notes (Task Model spec §5). Notes are supporting records, not events.
/// Reuses <see cref="ITaskService.GetAsync"/> so the caller's task visibility
/// (DataScope) is enforced before any note operation.
/// </summary>
public sealed class TaskNoteService(
    IRepository<TaskNote> notes,
    IRepository<User> users,
    ITaskService tasks,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskNoteService
{
    public async Task<Result<IReadOnlyList<TaskNoteDto>>> ListAsync(Guid taskId, CancellationToken ct = default)
    {
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure<IReadOnlyList<TaskNoteDto>>(TaskErrors.NotFound("Task"));
        var items = await (
            from n in notes.Query()
            where n.TaskId == taskId
            join u in users.Query() on n.AuthorId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby n.IsPinned descending, n.CreatedAt descending
            select new TaskNoteDto(n.Id, n.Body, n.IsPinned, n.IsInternal, n.AuthorId, u != null ? u.DisplayName : null, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskNoteDto>>(items);
    }

    public async Task<Result<Guid>> AddAsync(Guid taskId, CreateNoteRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<Guid>(TaskErrors.NoScope());
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure<Guid>(TaskErrors.NotFound("Task"));

        var note = new TaskNote(ws, taskId, request.Body, currentUser.UserId);
        note.Update(request.Body, request.IsPinned, request.IsInternal);
        notes.Add(note);
        await audit.LogAsync(Entry(AuditActions.Create, "TaskNote", note.Id, ws), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(note.Id);
    }

    public async Task<Result> UpdateAsync(Guid taskId, Guid noteId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure(TaskErrors.NotFound("Task"));
        var note = await notes.Query().FirstOrDefaultAsync(n => n.Id == noteId && n.TaskId == taskId, ct);
        if (note is null) return Result.Failure(TaskErrors.NotFound("Note"));
        note.Update(request.Body, request.IsPinned, request.IsInternal);
        await audit.LogAsync(Entry(AuditActions.Update, "TaskNote", note.Id, note.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveAsync(Guid taskId, Guid noteId, CancellationToken ct = default)
    {
        if ((await tasks.GetAsync(taskId, ct)).IsFailure) return Result.Failure(TaskErrors.NotFound("Task"));
        var note = await notes.Query().FirstOrDefaultAsync(n => n.Id == noteId && n.TaskId == taskId, ct);
        if (note is null) return Result.Failure(TaskErrors.NotFound("Note"));
        note.SoftDelete(currentUser.UserId, clock.UtcNow);
        await audit.LogAsync(Entry(AuditActions.Delete, "TaskNote", note.Id, note.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static AuditEntry Entry(string action, string resourceType, Guid id, Guid ws) => new()
    {
        Action = action, Module = "Tasks", ResourceType = resourceType, ResourceId = id.ToString(), WorkspaceId = ws,
    };
}
