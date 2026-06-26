using Erp.Application.Abstractions;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Auditing;
using Erp.Domain.Tasks;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Erp.Domain.Tasks.TaskStatus; // disambiguate from System.Threading.Tasks.TaskStatus

namespace Erp.Application.Tasks;

/// <summary>
/// Manages task status workflows (Task Model spec §4): named status types and the
/// ordered statuses inside them, with a single default workflow and a single
/// initial status per type. Statuses/types in use by tasks cannot be archived.
/// </summary>
public sealed class TaskWorkflowService(
    IRepository<TaskStatusType> statusTypes,
    IRepository<TaskStatus> statuses,
    IRepository<TaskItem> tasks,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskWorkflowService
{
    public async Task<Result<IReadOnlyList<TaskWorkflowDto>>> GetWorkflowsAsync(CancellationToken ct = default)
    {
        var types = await statusTypes.Query().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
        var all = await statuses.Query().OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);

        var result = types.Select(t => new TaskWorkflowDto(
            new TaskStatusTypeDto(t.Id, t.Name, t.Description, t.IsDefault, t.IsActive, t.SortOrder),
            all.Where(s => s.StatusTypeId == t.Id)
                .Select(s => new TaskStatusDto(s.Id, s.StatusTypeId, s.Name, s.Category, s.Color, s.SortOrder, s.IsInitial, s.IsFinal))
                .ToList()))
            .ToList();

        return Result.Success<IReadOnlyList<TaskWorkflowDto>>(result);
    }

    public async Task<Result<Guid>> CreateStatusTypeAsync(CreateStatusTypeRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<Guid>(TaskErrors.NoScope());

        var name = request.Name.Trim();
        if (await statusTypes.Query().AnyAsync(t => t.Name.ToLower() == name.ToLower(), ct))
            return Result.Failure<Guid>(TaskErrors.NameTaken());

        var isFirst = !await statusTypes.Query().AnyAsync(ct);
        var type = new TaskStatusType(ws, name);
        type.Update(name, request.Description, 0);
        type.SetDefault(isFirst);
        statusTypes.Add(type);

        await audit.LogAsync(Entry(AuditActions.Create, "TaskStatusType", type.Id, ws), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(type.Id);
    }

    public async Task<Result> UpdateStatusTypeAsync(Guid id, UpdateStatusTypeRequest request, CancellationToken ct = default)
    {
        var type = await statusTypes.GetByIdAsync(id, ct);
        if (type is null) return Result.Failure(TaskErrors.NotFound("Status type"));

        type.Update(request.Name, request.Description, request.SortOrder);
        type.SetActive(request.IsActive);
        if (request.IsDefault && !type.IsDefault)
        {
            foreach (var other in await statusTypes.Query().Where(t => t.IsDefault && t.Id != id).ToListAsync(ct))
                other.SetDefault(false);
        }
        type.SetDefault(request.IsDefault);

        await audit.LogAsync(Entry(AuditActions.Update, "TaskStatusType", type.Id, type.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ArchiveStatusTypeAsync(Guid id, CancellationToken ct = default)
    {
        var type = await statusTypes.GetByIdAsync(id, ct);
        if (type is null) return Result.Failure(TaskErrors.NotFound("Status type"));
        if (await tasks.Query().AnyAsync(t => t.StatusTypeId == id, ct))
            return Result.Failure(TaskErrors.WorkflowInUse());

        foreach (var status in await statuses.Query().Where(s => s.StatusTypeId == id).ToListAsync(ct))
            status.SoftDelete(currentUser.UserId, clock.UtcNow);
        type.SoftDelete(currentUser.UserId, clock.UtcNow);

        await audit.LogAsync(Entry(AuditActions.Delete, "TaskStatusType", type.Id, type.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<Guid>> CreateStatusAsync(CreateStatusRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<Guid>(TaskErrors.NoScope());

        var type = await statusTypes.GetByIdAsync(request.StatusTypeId, ct);
        if (type is null) return Result.Failure<Guid>(TaskErrors.NotFound("Status type"));

        var isFirst = !await statuses.Query().AnyAsync(s => s.StatusTypeId == type.Id, ct);
        var makeInitial = request.IsInitial || isFirst;
        if (makeInitial)
        {
            foreach (var other in await statuses.Query().Where(s => s.StatusTypeId == type.Id && s.IsInitial).ToListAsync(ct))
                other.SetInitial(false);
        }

        var status = new TaskStatus(ws, type.Id, request.Name, request.Category);
        status.Update(request.Name, request.Category, request.Color, 0);
        status.SetInitial(makeInitial);
        status.SetFinal(request.IsFinal);
        statuses.Add(status);

        await audit.LogAsync(Entry(AuditActions.Create, "TaskStatus", status.Id, ws), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(status.Id);
    }

    public async Task<Result> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        var status = await statuses.GetByIdAsync(id, ct);
        if (status is null) return Result.Failure(TaskErrors.NotFound("Status"));

        status.Update(request.Name, request.Category, request.Color, request.SortOrder);
        if (request.IsInitial && !status.IsInitial)
        {
            foreach (var other in await statuses.Query().Where(s => s.StatusTypeId == status.StatusTypeId && s.IsInitial && s.Id != id).ToListAsync(ct))
                other.SetInitial(false);
        }
        status.SetInitial(request.IsInitial);
        status.SetFinal(request.IsFinal);

        await audit.LogAsync(Entry(AuditActions.Update, "TaskStatus", status.Id, status.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ArchiveStatusAsync(Guid id, CancellationToken ct = default)
    {
        var status = await statuses.GetByIdAsync(id, ct);
        if (status is null) return Result.Failure(TaskErrors.NotFound("Status"));
        if (await tasks.Query().AnyAsync(t => t.StatusId == id, ct))
            return Result.Failure(TaskErrors.StatusInUse());

        status.SoftDelete(currentUser.UserId, clock.UtcNow);
        await audit.LogAsync(Entry(AuditActions.Delete, "TaskStatus", status.Id, status.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static AuditEntry Entry(string action, string resourceType, Guid id, Guid ws) => new()
    {
        Action = action, Module = "Tasks", ResourceType = resourceType,
        ResourceId = id.ToString(), WorkspaceId = ws,
    };
}
