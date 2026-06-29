using Erp.Application.Abstractions;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Auditing;
using Erp.Domain.Events;
using Erp.Domain.Workflow;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Tasks;

/// <summary>
/// Manages a workspace's task statuses and priorities (generic status_types/statuses).
/// Enforces the single-initial invariant for TASK_STATUS and blocks deletion of statuses
/// that are in use. All writes are audited.
/// </summary>
public sealed class TaskSettingsService(
    IRepository<Status> statuses,
    IRepository<StatusType> statusTypes,
    IRepository<EventStatus> eventStatuses,
    IRepository<TaskEvent> tasks,
    IRepository<TaskSettings> settings,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskSettingsService
{
    public async Task<Result<TaskSettingsDto>> GetSettingsAsync(CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<TaskSettingsDto>(TaskErrors.NoScope());
        var s = await settings.Query().FirstOrDefaultAsync(x => x.WorkspaceId == ws, ct) ?? new TaskSettings(ws);
        return Result.Success(new TaskSettingsDto(
            s.DailyReportRequired, s.AllowStatusChangeFromReport, s.RequireActualTime, s.RequireEstimatedTime,
            s.AllowMultipleReportsPerDay, s.NotifyOnTaskCreated, s.NotifyOnTaskAssigned, s.NotifyOnStatusChange,
            s.NotifyOnDailyReport, s.DashboardDefaultRangeDays));
    }

    public async Task<Result> UpdateSettingsAsync(UpdateTaskSettingsRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure(TaskErrors.NoScope());
        var s = await settings.Query().FirstOrDefaultAsync(x => x.WorkspaceId == ws, ct);
        if (s is null)
        {
            s = new TaskSettings(ws);
            settings.Add(s);
        }
        s.Update(request.DailyReportRequired, request.AllowStatusChangeFromReport, request.RequireActualTime,
            request.RequireEstimatedTime, request.AllowMultipleReportsPerDay, request.NotifyOnTaskCreated,
            request.NotifyOnTaskAssigned, request.NotifyOnStatusChange, request.NotifyOnDailyReport,
            request.DashboardDefaultRangeDays);
        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Update,
            Module = "Tasks",
            ResourceType = "TaskSettings",
            ResourceId = ws.ToString(),
            WorkspaceId = ws,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<StatusDto>>> ListAsync(string statusTypeCode, CancellationToken ct = default)
    {
        var list = await (
            from s in statuses.Query()
            join t in statusTypes.Query() on s.StatusTypeId equals t.Id
            where t.Code == statusTypeCode
            orderby s.SortOrder
            select new StatusDto(s.Id, s.Code, s.Name, s.Color, s.SortOrder, s.IsInitial, s.IsClosed, s.IsActive))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<StatusDto>>(list);
    }

    public async Task<Result<Guid>> CreateStatusAsync(CreateStatusRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<Guid>(TaskErrors.NoScope());

        var type = await statusTypes.Query().FirstOrDefaultAsync(t => t.Code == request.StatusTypeCode, ct);
        if (type is null) return Result.Failure<Guid>(TaskErrors.StatusTypeNotFound());

        var isStatusWorkflow = type.Code == StatusTypeCodes.TaskStatus;
        var isInitial = isStatusWorkflow && request.IsInitial;
        var isClosed = isStatusWorkflow && request.IsClosed;

        var existing = await statuses.Query().Where(s => s.StatusTypeId == type.Id)
            .Select(s => new { s.Code, s.SortOrder }).ToListAsync(ct);
        var code = UniqueCode(request.Name, existing.Select(e => e.Code));
        var sortOrder = existing.Count == 0 ? 0 : existing.Max(e => e.SortOrder) + 1;

        if (isInitial) await ClearInitialAsync(type.Id, ct);

        var status = new Status(ws, type.Id, code, request.Name, sortOrder, isInitial, isClosed, request.Color);
        statuses.Add(status);
        await audit.LogAsync(Entry(AuditActions.Create, status.Id, ws), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(status.Id);
    }

    public async Task<Result> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        var status = await statuses.Query().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (status is null) return Result.Failure(TaskErrors.NotFound("Status"));

        var type = await statusTypes.Query().FirstAsync(t => t.Id == status.StatusTypeId, ct);
        var isStatusWorkflow = type.Code == StatusTypeCodes.TaskStatus;
        var isInitial = isStatusWorkflow && request.IsInitial;
        var isClosed = isStatusWorkflow && request.IsClosed;

        if (isInitial) await ClearInitialAsync(type.Id, ct);
        status.Update(request.Name, status.SortOrder, isInitial, isClosed, request.Color, request.IsActive);
        await audit.LogAsync(Entry(AuditActions.Update, status.Id, status.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReorderAsync(ReorderStatusesRequest request, CancellationToken ct = default)
    {
        var type = await statusTypes.Query().FirstOrDefaultAsync(t => t.Code == request.StatusTypeCode, ct);
        if (type is null) return Result.Failure(TaskErrors.StatusTypeNotFound());

        var byId = await statuses.Query().Where(s => s.StatusTypeId == type.Id).ToDictionaryAsync(s => s.Id, ct);
        for (var i = 0; i < request.OrderedIds.Count; i++)
            if (byId.TryGetValue(request.OrderedIds[i], out var s)) s.SetSortOrder(i);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteStatusAsync(Guid id, CancellationToken ct = default)
    {
        var status = await statuses.Query().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (status is null) return Result.Failure(TaskErrors.NotFound("Status"));
        if (status.IsInitial) return Result.Failure(TaskErrors.StatusIsInitial());

        var inUse = await eventStatuses.Query().AnyAsync(es => es.StatusId == id, ct)
            || await tasks.Query().AnyAsync(te => te.PriorityStatusId == id, ct);
        if (inUse) return Result.Failure(TaskErrors.StatusInUse());

        status.SoftDelete(currentUser.UserId, clock.UtcNow);
        await audit.LogAsync(Entry(AuditActions.Delete, status.Id, status.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task ClearInitialAsync(Guid statusTypeId, CancellationToken ct)
    {
        var current = await statuses.Query().Where(s => s.StatusTypeId == statusTypeId && s.IsInitial).ToListAsync(ct);
        foreach (var s in current) s.ClearInitial();
    }

    private static string UniqueCode(string name, IEnumerable<string> taken)
    {
        var baseCode = new string(name.Trim().ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray())
            .Trim('_');
        if (string.IsNullOrEmpty(baseCode)) baseCode = "STATUS";
        var set = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!set.Contains(baseCode)) return baseCode;
        for (var i = 2; ; i++)
        {
            var candidate = $"{baseCode}_{i}";
            if (!set.Contains(candidate)) return candidate;
        }
    }

    private static AuditEntry Entry(string action, Guid statusId, Guid ws) => new()
    {
        Action = action,
        Module = "Tasks",
        ResourceType = "TaskStatus",
        ResourceId = statusId.ToString(),
        WorkspaceId = ws,
    };
}
