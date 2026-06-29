using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Assets;
using Erp.Domain.Auditing;
using Erp.Domain.Authorization;
using Erp.Domain.Events;
using Erp.Domain.Tasks;
using Erp.Domain.Identity;
using Erp.Domain.Mailing;
using Erp.Domain.Structure;
using Erp.Domain.Statuses;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Tasks;

/// <summary>
/// Task Management on the Event/Asset architecture. A task = Event(TASK_MANAGEMENT) +
/// TaskEvent; status history in EventStatus; priority a Status under TASK_PRIORITY;
/// notes/documents are Assets linked via EventAsset; dependencies are EventDependency.
/// Reads respect the caller's task.view DataScope; writes are audited + logged.
/// </summary>
public sealed class TaskService(
    IRepository<Event> events,
    IRepository<TaskEvent> tasks,
    IRepository<EventStatus> eventStatuses,
    IRepository<EventActivity> activities,
    IRepository<EventDependency> dependencies,
    IRepository<EventDailyReport> dailyReports,
    IRepository<TaskSettings> taskSettings,
    IRepository<Status> statuses,
    IRepository<StatusType> statusTypes,
    IRepository<EventType> eventTypes,
    IRepository<Asset> assets,
    IRepository<AssetType> assetTypes,
    IRepository<Note> notes,
    IRepository<Document> documents,
    IRepository<EventAsset> eventAssets,
    IRepository<User> users,
    IRepository<Employee> employees,
    IRepository<StructureNode> structureNodes,
    IRepository<AuditLog> auditLogs,
    ITaskReadRepository taskRead,
    IPermissionResolver permissions,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IMailOutbox mailOutbox,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskService
{
    // ---- Reads ------------------------------------------------------------

    public async Task<Result<PagedResult<TaskListItemDto>>> ListAsync(TaskListQuery query, CancellationToken ct = default)
    {
        if (await ResolveScopeAsync(ct) is not { } scope) return Result.Failure<PagedResult<TaskListItemDto>>(TaskErrors.NoScope());
        return Result.Success(await taskRead.ListAsync(scope, query, ct));
    }

    public async Task<Result<TaskDetailsDto>> GetAsync(long eventId, CancellationToken ct = default)
    {
        if (await ResolveScopeAsync(ct) is not { } scope) return Result.Failure<TaskDetailsDto>(TaskErrors.NoScope());
        var dto = await taskRead.GetDetailsAsync(scope, eventId, ct);
        return dto is null ? Result.Failure<TaskDetailsDto>(TaskErrors.NotFound("Task")) : Result.Success(dto);
    }

    public async Task<Result<IReadOnlyList<TaskListItemDto>>> ListSubtasksAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskListItemDto>>(TaskErrors.NotFound("Task"));
        var now = clock.UtcNow;
        var list = await BaseQuery().Where(x => x.te.ParentEventId == eventId)
            .OrderBy(x => x.te.InsertedDate).Select(Projection(now)).ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskListItemDto>>(list);
    }

    public async Task<Result<MyTasksGroups>> GetMyTasksAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } me) return Result.Failure<MyTasksGroups>(TaskErrors.NoScope());
        var now = clock.UtcNow;
        var today = now.Date;

        var mine = await BaseQuery().Where(x => x.te.AssigneeId == me && (x.st == null || !x.st.IsClosed))
            .OrderBy(x => x.te.DueAt).Select(Projection(now)).ToListAsync(ct);

        List<TaskListItemDto> overdue = [], dueToday = [], upcoming = [], waiting = [];
        foreach (var t in mine)
        {
            if (t.DueAt is not { } due) { waiting.Add(t); continue; }
            var d = due.Date;
            if (d < today) overdue.Add(t);
            else if (d == today) dueToday.Add(t);
            else upcoming.Add(t);
        }
        return Result.Success(new MyTasksGroups(overdue, dueToday, upcoming, waiting));
    }

    public async Task<Result<TaskDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        if (await ResolveScopeAsync(ct) is not { } scope) return Result.Failure<TaskDashboardDto>(TaskErrors.NoScope());
        return Result.Success(await taskRead.GetDashboardAsync(scope, ct));
    }

    public async Task<Result<TaskReportDto>> GetReportAsync(TaskListQuery query, CancellationToken ct = default)
    {
        if (await ResolveScopeAsync(ct) is not { } scope) return Result.Failure<TaskReportDto>(TaskErrors.NoScope());
        return Result.Success(await taskRead.GetReportAsync(scope, query, ct));
    }

    public async Task<Result<PagedResult<TaskDailyReportRowDto>>> GetDailyReportsReportAsync(TaskDailyReportQuery query, CancellationToken ct = default)
    {
        if (await ResolveScopeAsync(ct) is not { } scope) return Result.Failure<PagedResult<TaskDailyReportRowDto>>(TaskErrors.NoScope());
        return Result.Success(await taskRead.GetDailyReportsAsync(scope, query, ct));
    }

    /// <summary>Resolves the caller's visible task scope (workspace + DataScope user set) for the read repository.</summary>
    private async Task<VisibleScope?> ResolveScopeAsync(CancellationToken ct)
    {
        if (currentUser.WorkspaceId is not { } ws || currentUser.UserId is not { } me) return null;
        var scope = (await permissions.ResolveAsync(me, ct)).ScopeFor(PermissionCatalog.TaskView) ?? DataScope.Own;
        return await taskRead.GetVisibleScopeAsync(ws, me, scope, ct);
    }

    public async Task<Result<IReadOnlyList<TaskActivityDto>>> GetActivityAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskActivityDto>>(TaskErrors.NotFound("Task"));
        var list = await (
            from a in activities.Query()
            where a.EventId == eventId
            join u in users.Query() on a.ActorId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby a.OccurredAt descending
            select new TaskActivityDto(a.Id, a.Kind, a.Message, a.ActorId, u != null ? u.DisplayName : null, a.OccurredAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskActivityDto>>(list);
    }

    public async Task<Result<IReadOnlyList<TaskAuditDto>>> GetAuditAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskAuditDto>>(TaskErrors.NotFound("Task"));
        var id = eventId.ToString();
        var list = await auditLogs.Query()
            .Where(a => a.ResourceType == "Task" && a.ResourceId == id)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new TaskAuditDto(a.Id, a.Action, a.OccurredAt, a.ActorUserId, a.ActorDisplayName))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskAuditDto>>(list);
    }

    public async Task<Result<IReadOnlyList<StatusDto>>> ListStatusesAsync(string statusTypeCode, CancellationToken ct = default)
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

    // ---- Writes -----------------------------------------------------------

    public Task<Result<CreateTaskResult>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
        => CreateInternalAsync(request, parentEventId: null, ct);

    public async Task<Result<CreateTaskResult>> CreateSubtaskAsync(long parentEventId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var parent = await GetVisibleTaskAsync(parentEventId, ct);
        if (parent is null) return Result.Failure<CreateTaskResult>(TaskErrors.NotFound("Parent task"));
        if (await IsClosedAsync(parentEventId, ct)) return Result.Failure<CreateTaskResult>(TaskErrors.Closed());
        return await CreateInternalAsync(request, parentEventId, ct);
    }

    private async Task<Result<CreateTaskResult>> CreateInternalAsync(CreateTaskRequest request, long? parentEventId, CancellationToken ct)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<CreateTaskResult>(TaskErrors.NoScope());

        var eventTypeId = await eventTypes.Query()
            .Where(t => t.Code == EventTypeCodes.TaskManagement).Select(t => (long?)t.Id).FirstOrDefaultAsync(ct);
        if (eventTypeId is null) return Result.Failure<CreateTaskResult>(TaskErrors.NoStatuses());

        var initial = await (from s in statuses.Query()
                             join t in statusTypes.Query() on s.StatusTypeId equals t.Id
                             where t.Code == StatusTypeCodes.TaskStatus && s.IsActive
                             orderby s.IsInitial descending, s.SortOrder
                             select s).FirstOrDefaultAsync(ct);
        if (initial is null) return Result.Failure<CreateTaskResult>(TaskErrors.NoInitialStatus());

        if (request.PriorityStatusId is { } pri && !await IsPriorityAsync(pri, ct))
            return Result.Failure<CreateTaskResult>(TaskErrors.PriorityInvalid());

        var ev = new Event(ws, eventTypeId.Value);
        events.Add(ev);

        var reference = await NextReferenceNoAsync(ct);
        var te = new TaskEvent(ws, ev.Id, reference, request.Title, currentUser.UserId);
        te.UpdateDetails(request.Title, request.Description);
        te.SetSchedule(request.StartAt, request.DueAt, request.EstimatedTime);
        te.Assign(request.AssigneeId);
        te.SetPriority(request.PriorityStatusId);
        if (parentEventId is { } p) te.PlaceUnderParent(p);
        tasks.Add(te);

        eventStatuses.Add(new EventStatus(ws, ev.Id, initial.Id, null));
        AddActivity(ws, ev.Id, parentEventId is null ? EventActivityKind.Created : EventActivityKind.SubtaskAdded, $"Task {reference} created.");
        if (parentEventId is { } pe) AddActivity(ws, pe, EventActivityKind.SubtaskAdded, $"Subtask {reference} added.");

        await audit.LogAsync(Entry(AuditActions.Create, ev.Id, ws, $"{{\"reference\":\"{reference}\"}}"), ct);
        if ((await GetSettingsAsync(ws, ct)).NotifyOnTaskCreated)
            await NotifyAsync(te, MailTemplateCodes.TaskCreated, [request.AssigneeId], NoExtras, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateTaskResult(ev.Id, reference));
    }

    public async Task<Result> UpdateAsync(long eventId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(eventId, ct)) return Result.Failure(TaskErrors.Closed());

        te.UpdateDetails(request.Title, request.Description);
        te.SetSchedule(request.StartAt, request.DueAt, request.EstimatedTime);
        te.SetCompletion(request.CompletionPercent, request.ActualTime);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.Updated, "Task details updated.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(long eventId, ChangeStatusRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure(TaskErrors.NotFound("Task"));

        var status = await (from s in statuses.Query()
                            join t in statusTypes.Query() on s.StatusTypeId equals t.Id
                            where s.Id == request.StatusId && t.Code == StatusTypeCodes.TaskStatus
                            select s).FirstOrDefaultAsync(ct);
        if (status is null) return Result.Failure(TaskErrors.StatusInvalid());

        var from = await SupersedeStatusAsync(eventId, te.WorkspaceId, status.Id, request.Note, ct);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.StatusChanged, $"Status changed to {status.Name}.", from?.Id, status.Id);
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId, $"{{\"status\":\"{Escape(status.Name)}\"}}"), ct);

        if ((await GetSettingsAsync(te.WorkspaceId, ct)).NotifyOnStatusChange)
        {
            var (code, extras) = StatusMail(status, from);
            await NotifyAsync(te, code, [te.AssigneeId, te.ReporterId], extras, ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Effective task settings for the workspace (a transient default when none is saved yet).</summary>
    private async Task<TaskSettings> GetSettingsAsync(long workspaceId, CancellationToken ct) =>
        await taskSettings.Query().FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct)
        ?? new TaskSettings(workspaceId);

    /// <summary>Supersedes the current event status (if any) and inserts the new one as current. Returns the previous Status.</summary>
    private async Task<Status?> SupersedeStatusAsync(long eventId, long ws, long newStatusId, string? note, CancellationToken ct)
    {
        var current = await eventStatuses.Query().FirstOrDefaultAsync(es => es.EventId == eventId && es.IsCurrent, ct);
        Status? from = null;
        if (current is not null)
        {
            from = await statuses.GetByIdAsync(current.StatusId, ct);
            current.Supersede();
        }
        eventStatuses.Add(new EventStatus(ws, eventId, newStatusId, note));
        return from;
    }

    /// <summary>Chooses the notification template for a status transition (opened/completed/changed) + placeholders.</summary>
    private static (string code, IReadOnlyDictionary<string, string> extras) StatusMail(Status to, Status? from)
    {
        var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Status"] = to.Name,
            ["OldStatus"] = from?.Name ?? string.Empty,
        };
        var code = to.IsClosed ? MailTemplateCodes.TaskCompleted
            : from is { IsInitial: true } && !to.IsInitial ? MailTemplateCodes.TaskOpened
            : MailTemplateCodes.TaskStatusChanged;
        return (code, extras);
    }

    public async Task<Result> AssignAsync(long eventId, AssignTaskRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure(TaskErrors.NotFound("Task"));
        if (request.AssigneeId is { } aid && !await users.Query().AnyAsync(u => u.Id == aid, ct))
            return Result.Failure(TaskErrors.NotFound("Assignee"));

        te.Assign(request.AssigneeId);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.Assigned, request.AssigneeId is null ? "Task unassigned." : "Task assigned.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        if (request.AssigneeId is not null && (await GetSettingsAsync(te.WorkspaceId, ct)).NotifyOnTaskAssigned)
            await NotifyAsync(te, MailTemplateCodes.TaskAssigned, [request.AssigneeId], NoExtras, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetPriorityAsync(long eventId, SetPriorityRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure(TaskErrors.NotFound("Task"));
        if (request.PriorityStatusId is { } pri && !await IsPriorityAsync(pri, ct))
            return Result.Failure(TaskErrors.PriorityInvalid());

        te.SetPriority(request.PriorityStatusId);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.PriorityChanged, "Priority changed.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(long eventId, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var ev = await events.GetByIdAsync(eventId, ct);

        var actor = currentUser.UserId;
        var when = clock.UtcNow;
        te.SoftDelete(actor, when);
        ev?.SoftDelete(actor, when);
        await audit.LogAsync(Entry(AuditActions.Delete, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Notes (Asset pattern) -------------------------------------------

    public async Task<Result<IReadOnlyList<TaskNoteDto>>> ListNotesAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskNoteDto>>(TaskErrors.NotFound("Task"));
        var list = await (
            from ea in eventAssets.Query()
            where ea.EventId == eventId && ea.RelationType == EventAssetRelationTypes.Note
            join n in notes.Query() on ea.AssetId equals n.AssetId
            join u in users.Query() on n.InsertedBy equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby n.IsPinned descending, n.InsertedDate descending
            select new TaskNoteDto(n.Id, n.Body, n.IsPinned, n.IsInternal, n.InsertedBy, u != null ? u.DisplayName : null, n.InsertedDate))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskNoteDto>>(list);
    }

    public async Task<Result<long>> AddNoteAsync(long eventId, CreateNoteRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure<long>(TaskErrors.NotFound("Task"));
        var assetTypeId = await AssetTypeIdAsync(AssetTypeCodes.Note, ct);
        if (assetTypeId is null) return Result.Failure<long>(TaskErrors.NotFound("Asset type"));

        var asset = new Asset(te.WorkspaceId, assetTypeId.Value, "Note", null);
        assets.Add(asset);
        var note = new Note(te.WorkspaceId, asset.Id, request.Body, request.IsPinned, request.IsInternal);
        notes.Add(note);
        eventAssets.Add(new EventAsset(te.WorkspaceId, eventId, asset.Id, EventAssetRelationTypes.Note, null));
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.NoteAdded, "Note added.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(note.Id);
    }

    public async Task<Result> UpdateNoteAsync(long eventId, long noteId, UpdateNoteRequest request, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var note = await NoteForEventAsync(eventId, noteId, ct);
        if (note is null) return Result.Failure(TaskErrors.NotFound("Note"));
        note.Update(request.Body, request.IsPinned, request.IsInternal);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveNoteAsync(long eventId, long noteId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var note = await NoteForEventAsync(eventId, noteId, ct);
        if (note is null) return Result.Failure(TaskErrors.NotFound("Note"));
        await RemoveAssetLinkAsync(eventId, note.AssetId, ct);
        note.SoftDelete(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Documents (Asset pattern) ---------------------------------------

    public async Task<Result<IReadOnlyList<TaskDocumentDto>>> ListDocumentsAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskDocumentDto>>(TaskErrors.NotFound("Task"));
        var list = await (
            from ea in eventAssets.Query()
            where ea.EventId == eventId && ea.RelationType == EventAssetRelationTypes.Document
            join d in documents.Query() on ea.AssetId equals d.AssetId
            join u in users.Query() on d.InsertedBy equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby d.InsertedDate descending
            select new TaskDocumentDto(d.Id, d.FileName, d.FilePath, d.MimeType, d.InsertedBy, u != null ? u.DisplayName : null, d.InsertedDate))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskDocumentDto>>(list);
    }

    public async Task<Result<long>> AddDocumentAsync(long eventId, CreateDocumentRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure<long>(TaskErrors.NotFound("Task"));
        var assetTypeId = await AssetTypeIdAsync(AssetTypeCodes.Document, ct);
        if (assetTypeId is null) return Result.Failure<long>(TaskErrors.NotFound("Asset type"));

        var asset = new Asset(te.WorkspaceId, assetTypeId.Value, request.FileName, null);
        assets.Add(asset);
        var doc = new Document(te.WorkspaceId, asset.Id, request.FileName, request.FilePath, request.MimeType, null);
        documents.Add(doc);
        eventAssets.Add(new EventAsset(te.WorkspaceId, eventId, asset.Id, EventAssetRelationTypes.Document, null));
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.DocumentAdded, "Document added.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(doc.Id);
    }

    public async Task<Result> RemoveDocumentAsync(long eventId, long documentId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var doc = await (from ea in eventAssets.Query()
                         where ea.EventId == eventId && ea.RelationType == EventAssetRelationTypes.Document
                         join d in documents.Query() on ea.AssetId equals d.AssetId
                         where d.Id == documentId
                         select d).FirstOrDefaultAsync(ct);
        if (doc is null) return Result.Failure(TaskErrors.NotFound("Document"));
        await RemoveAssetLinkAsync(eventId, doc.AssetId, ct);
        doc.SoftDelete(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Dependencies -----------------------------------------------------

    public async Task<Result<IReadOnlyList<TaskDependencyDto>>> ListDependenciesAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskDependencyDto>>(TaskErrors.NotFound("Task"));
        var list = await (
            from d in dependencies.Query()
            where d.EventId == eventId
            join te in tasks.Query() on d.DependsOnEventId equals te.EventId
            select new TaskDependencyDto(d.Id, d.DependsOnEventId, te.ReferenceNo, te.Title, d.IsBlocking))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskDependencyDto>>(list);
    }

    public async Task<Result<long>> AddDependencyAsync(long eventId, CreateDependencyRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure<long>(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(eventId, ct)) return Result.Failure<long>(TaskErrors.Closed());
        if (request.DependsOnEventId == eventId) return Result.Failure<long>(TaskErrors.SelfDependency());
        if (!await tasks.Query().AnyAsync(t => t.EventId == request.DependsOnEventId, ct))
            return Result.Failure<long>(TaskErrors.NotFound("Dependency task"));
        if (await dependencies.Query().AnyAsync(d => d.EventId == eventId && d.DependsOnEventId == request.DependsOnEventId, ct))
            return Result.Failure<long>(TaskErrors.DuplicateLink());

        var dep = new EventDependency(te.WorkspaceId, eventId, request.DependsOnEventId, request.IsBlocking);
        dependencies.Add(dep);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.RelationChanged, "Dependency added.");
        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(dep.Id);
    }

    public async Task<Result> RemoveDependencyAsync(long eventId, long dependencyId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var dep = await dependencies.Query().FirstOrDefaultAsync(d => d.Id == dependencyId && d.EventId == eventId, ct);
        if (dep is null) return Result.Failure(TaskErrors.NotFound("Dependency"));
        dep.SoftDelete(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Daily reports ----------------------------------------------------

    public async Task<Result<IReadOnlyList<TaskDailyReportDto>>> ListDailyReportsAsync(long eventId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure<IReadOnlyList<TaskDailyReportDto>>(TaskErrors.NotFound("Task"));
        var list = await (
            from r in dailyReports.Query()
            where r.EventId == eventId
            join u in users.Query() on r.UserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            join s in statuses.Query() on r.StatusId equals s.Id into sg
            from s in sg.DefaultIfEmpty()
            orderby r.ReportDate descending, r.InsertedDate descending
            select new TaskDailyReportDto(r.Id, r.ReportDate, r.Description, r.EstimatedTime, r.ActualTime, r.RemainingTime,
                r.StatusId, s != null ? s.Name : null, s != null ? s.Color : null,
                r.UserId, u != null ? u.DisplayName : null, r.InsertedDate))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskDailyReportDto>>(list);
    }

    public async Task<Result<long>> AddDailyReportAsync(long eventId, CreateDailyReportRequest request, CancellationToken ct = default)
    {
        var te = await GetVisibleTaskAsync(eventId, ct);
        if (te is null) return Result.Failure<long>(TaskErrors.NotFound("Task"));

        var settings = await GetSettingsAsync(te.WorkspaceId, ct);
        if (settings.RequireEstimatedTime && request.EstimatedTime is null) return Result.Failure<long>(TaskErrors.ReportTimeRequired("estimated"));
        if (settings.RequireActualTime && request.ActualTime is null) return Result.Failure<long>(TaskErrors.ReportTimeRequired("actual"));

        var author = currentUser.UserId;
        var date = request.ReportDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        if (!settings.AllowMultipleReportsPerDay &&
            await dailyReports.Query().AnyAsync(r => r.EventId == eventId && r.ReportDate == date && r.UserId == author, ct))
            return Result.Failure<long>(TaskErrors.DuplicateReport());

        // Validate any selected status belongs to TASK_STATUS.
        Status? selected = null;
        if (request.StatusId is { } sid)
        {
            selected = await (from s in statuses.Query()
                              join t in statusTypes.Query() on s.StatusTypeId equals t.Id
                              where s.Id == sid && t.Code == StatusTypeCodes.TaskStatus
                              select s).FirstOrDefaultAsync(ct);
            if (selected is null) return Result.Failure<long>(TaskErrors.StatusInvalid());
        }

        var report = new EventDailyReport(te.WorkspaceId, eventId, author, date, request.Description,
            request.EstimatedTime, request.ActualTime, request.RemainingTime, request.StatusId);
        dailyReports.Add(report);
        AddActivity(te.WorkspaceId, eventId, EventActivityKind.DailyReportAdded, $"Daily report filed for {date:yyyy-MM-dd}.");

        // If the report selected a status that changes the current task status, record a status change.
        Status? from = null;
        var statusChanged = false;
        if (selected is not null && settings.AllowStatusChangeFromReport)
        {
            var current = await eventStatuses.Query().FirstOrDefaultAsync(es => es.EventId == eventId && es.IsCurrent, ct);
            if (current is null || current.StatusId != selected.Id)
            {
                from = current is not null ? await statuses.GetByIdAsync(current.StatusId, ct) : null;
                current?.Supersede();
                eventStatuses.Add(new EventStatus(te.WorkspaceId, eventId, selected.Id, $"Daily report {date:yyyy-MM-dd}"));
                AddActivity(te.WorkspaceId, eventId, EventActivityKind.StatusChanged, $"Status changed to {selected.Name}.", from?.Id, selected.Id);
                statusChanged = true;
            }
        }

        await audit.LogAsync(Entry(AuditActions.Update, eventId, te.WorkspaceId), ct);

        var extras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Date"] = date.ToString("yyyy-MM-dd"),
            ["DailyReportDescription"] = request.Description,
            ["Status"] = selected?.Name ?? string.Empty,
            ["OldStatus"] = from?.Name ?? string.Empty,
        };
        var notify = statusChanged ? settings.NotifyOnStatusChange : settings.NotifyOnDailyReport;
        if (notify)
        {
            var code = statusChanged ? MailTemplateCodes.DailyReportStatusChanged : MailTemplateCodes.DailyReportSubmitted;
            await NotifyAsync(te, code, [te.ReporterId, te.AssigneeId], extras, ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(report.Id);
    }

    public async Task<Result> UpdateDailyReportAsync(long eventId, long reportId, UpdateDailyReportRequest request, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var report = await dailyReports.Query().FirstOrDefaultAsync(r => r.Id == reportId && r.EventId == eventId, ct);
        if (report is null) return Result.Failure(TaskErrors.NotFound("Daily report"));

        // Moving to a day the author already reported would collide with the unique index.
        if (request.ReportDate != report.ReportDate &&
            await dailyReports.Query().AnyAsync(r => r.EventId == eventId && r.ReportDate == request.ReportDate
                && r.UserId == report.UserId && r.Id != reportId, ct))
            return Result.Failure(TaskErrors.DuplicateReport());

        report.Update(request.ReportDate, request.Description, request.EstimatedTime, request.ActualTime, request.RemainingTime, request.StatusId);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveDailyReportAsync(long eventId, long reportId, CancellationToken ct = default)
    {
        if (await GetVisibleTaskAsync(eventId, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var report = await dailyReports.Query().FirstOrDefaultAsync(r => r.Id == reportId && r.EventId == eventId, ct);
        if (report is null) return Result.Failure(TaskErrors.NotFound("Daily report"));
        report.SoftDelete(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Helpers ----------------------------------------------------------

    private IQueryable<TaskRow> BaseQuery() =>
        from te in tasks.Query()
        join e in events.Query() on te.EventId equals e.Id
        join esCur in eventStatuses.Query().Where(es => es.IsCurrent) on te.EventId equals esCur.EventId into esg
        from esCur in esg.DefaultIfEmpty()
        join st in statuses.Query() on esCur.StatusId equals st.Id into stg
        from st in stg.DefaultIfEmpty()
        join pr in statuses.Query() on te.PriorityStatusId equals pr.Id into prg
        from pr in prg.DefaultIfEmpty()
        join au in users.Query() on te.AssigneeId equals au.Id into aug
        from au in aug.DefaultIfEmpty()
        select new TaskRow { te = te, st = st, pr = pr, au = au, statusSince = (DateTimeOffset?)esCur.InsertedDate };

    private static System.Linq.Expressions.Expression<Func<TaskRow, TaskListItemDto>> Projection(DateTimeOffset now) =>
        x => new TaskListItemDto(
            x.te.EventId, x.te.ReferenceNo, x.te.Title,
            x.st != null ? x.st.Id : (long?)null, x.st != null ? x.st.Name : null, x.st != null ? x.st.Color : null, x.st != null && x.st.IsClosed,
            x.te.PriorityStatusId, x.pr != null ? x.pr.Name : null, x.pr != null ? x.pr.Color : null,
            x.te.AssigneeId, x.au != null ? x.au.DisplayName : null,
            x.te.DueAt, x.te.DueAt != null && x.te.DueAt < now && (x.st == null || !x.st.IsClosed),
            x.te.CompletionPercent, x.te.InsertedDate);

    private sealed class TaskRow
    {
        public TaskEvent te { get; set; } = default!;
        public Status? st { get; set; }
        public Status? pr { get; set; }
        public User? au { get; set; }
        /// <summary>When the current status was set (for "completed recently" analytics).</summary>
        public DateTimeOffset? statusSince { get; set; }
    }

    private async Task<TaskEvent?> GetVisibleTaskAsync(long eventId, CancellationToken ct)
    {
        var te = await tasks.Query().FirstOrDefaultAsync(x => x.EventId == eventId, ct);
        if (te is null) return null;
        var visible = await VisibleAssigneeFilterAsync(ct);
        if (visible is null) return te;
        var me = currentUser.UserId;
        var ok = te.AssigneeId == me || te.ReporterId == me || (te.AssigneeId is { } a && visible.Contains(a));
        return ok ? te : null;
    }

    private async Task<bool> IsClosedAsync(long eventId, CancellationToken ct) =>
        await (from es in eventStatuses.Query()
               where es.EventId == eventId && es.IsCurrent
               join s in statuses.Query() on es.StatusId equals s.Id
               select s.IsClosed).FirstOrDefaultAsync(ct);

    private async Task<bool> IsPriorityAsync(long statusId, CancellationToken ct) =>
        await (from s in statuses.Query()
               join t in statusTypes.Query() on s.StatusTypeId equals t.Id
               where s.Id == statusId && t.Code == StatusTypeCodes.TaskPriority
               select s.Id).AnyAsync(ct);

    private async Task<long?> AssetTypeIdAsync(string code, CancellationToken ct) =>
        await assetTypes.Query().Where(a => a.Code == code).Select(a => (long?)a.Id).FirstOrDefaultAsync(ct);

    private Task<Note?> NoteForEventAsync(long eventId, long noteId, CancellationToken ct) =>
        (from ea in eventAssets.Query()
         where ea.EventId == eventId && ea.RelationType == EventAssetRelationTypes.Note
         join n in notes.Query() on ea.AssetId equals n.AssetId
         where n.Id == noteId
         select n).FirstOrDefaultAsync(ct);

    private async Task RemoveAssetLinkAsync(long eventId, long assetId, CancellationToken ct)
    {
        var when = clock.UtcNow;
        var actor = currentUser.UserId;
        var link = await eventAssets.Query().FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.AssetId == assetId, ct);
        link?.SoftDelete(actor, when);
        var asset = await assets.GetByIdAsync(assetId, ct);
        asset?.SoftDelete(actor, when);
    }

    private async Task<string> NextReferenceNoAsync(CancellationToken ct)
    {
        var seq = await tasks.Query().CountAsync(ct) + 1;
        return $"TSK-{seq:D5}";
    }

    private void AddActivity(long ws, long eventId, EventActivityKind kind, string message, long? fromStatusId = null, long? toStatusId = null)
    {
        var act = new EventActivity(ws, eventId, kind, message, currentUser.UserId, clock.UtcNow);
        if (fromStatusId is not null || toStatusId is not null) act.WithStatusChange(fromStatusId, toStatusId);
        activities.Add(act);
    }

    /// <summary>
    /// Queues a task notification onto the mail outbox (best-effort; never aborts the operation).
    /// Recipients are resolved to their email addresses, excluding the acting user. Persisted by
    /// the caller's SaveChanges and delivered later by the dispatcher worker.
    /// </summary>
    private async Task NotifyAsync(TaskEvent te, string templateCode, IEnumerable<long?> recipientUserIds,
        IReadOnlyDictionary<string, string> extraPlaceholders, CancellationToken ct)
    {
        try
        {
            var me = currentUser.UserId;
            var ids = recipientUserIds.Where(id => id is { } && id != me).Select(id => id!.Value).Distinct().ToList();
            if (ids.Count == 0) return;

            var people = await users.Query()
                .Where(u => ids.Contains(u.Id) && u.Email != null)
                .Select(u => new { u.Email, u.DisplayName }).ToListAsync(ct);
            var recipients = people
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .Select(p => new MailRecipientInput(p.Email!, p.DisplayName))
                .ToList();
            if (recipients.Count == 0) return;

            var actorName = me is { } a
                ? await users.Query().Where(u => u.Id == a).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) ?? "Someone"
                : "Someone";
            var priorityName = te.PriorityStatusId is { } pid
                ? await statuses.Query().Where(s => s.Id == pid).Select(s => s.Name).FirstOrDefaultAsync(ct) : null;
            var assigneeName = te.AssigneeId is { } aid
                ? await users.Query().Where(u => u.Id == aid).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) : null;
            var reporterName = te.ReporterId is { } rid
                ? await users.Query().Where(u => u.Id == rid).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) : null;
            var dueDate = te.DueAt?.ToString("yyyy-MM-dd") ?? string.Empty;

            // Provide both our placeholder names and the doc's names (Mail doc §5/§12) as aliases.
            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TaskRef"] = te.ReferenceNo,
                ["ReferenceNo"] = te.ReferenceNo,
                ["TaskTitle"] = te.Title,
                ["Actor"] = actorName,
                ["UserName"] = actorName,
                ["AssigneeName"] = assigneeName ?? string.Empty,
                ["ReporterName"] = reporterName ?? string.Empty,
                ["Priority"] = priorityName ?? string.Empty,
                ["PriorityName"] = priorityName ?? string.Empty,
                ["DueDate"] = dueDate,
                ["Date"] = clock.UtcNow.ToString("yyyy-MM-dd"),
                ["ReportDate"] = clock.UtcNow.ToString("yyyy-MM-dd"),
            };
            foreach (var kv in extraPlaceholders) placeholders[kv.Key] = kv.Value;
            if (placeholders.TryGetValue("Status", out var st)) placeholders["NewStatus"] = st;

            var fallbackSubject = $"{te.ReferenceNo}: {te.Title}";
            var fallbackBody = $"<p>Update on <strong>{te.ReferenceNo} — {te.Title}</strong> by {actorName}.</p>";

            await mailOutbox.QueueAsync(new MailRequest(te.WorkspaceId, templateCode, fallbackSubject, fallbackBody,
                placeholders, recipients), ct);
        }
        catch
        {
            // Notifications are best-effort; a queueing failure must not roll back the task operation.
        }
    }

    private static readonly IReadOnlyDictionary<string, string> NoExtras =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static AuditEntry Entry(string action, long eventId, long ws, string? newValues = null) => new()
    {
        Action = action,
        Module = "Tasks",
        ResourceType = "Task",
        ResourceId = eventId.ToString(),
        WorkspaceId = ws,
        NewValues = newValues,
    };

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ---- DataScope visibility (own/team/department/broader) ----------------

    private async Task<HashSet<long>?> VisibleAssigneeFilterAsync(CancellationToken ct)
    {
        if (currentUser.UserId is not { } me) return [];
        var scope = (await permissions.ResolveAsync(me, ct)).ScopeFor(PermissionCatalog.TaskView) ?? DataScope.Own;
        if (scope >= DataScope.Cluster) return null; // sees everything in the workspace
        if (scope == DataScope.Own) return [me];

        var myNode = await employees.Query().Where(e => e.UserId == me).Select(e => e.PlacementNodeId).FirstOrDefaultAsync(ct);
        if (myNode is not { } nodeId) return [me];

        var nodes = await structureNodes.Query().Select(n => new NodeRef(n.Id, n.ParentId, n.NodeType)).ToListAsync(ct);
        var root = scope == DataScope.Department ? NearestDepartment(nodes, nodeId) : nodeId;
        var subtree = Descendants(nodes, root);
        var ids = await employees.Query()
            .Where(e => e.PlacementNodeId != null && subtree.Contains(e.PlacementNodeId.Value))
            .Select(e => e.UserId).ToListAsync(ct);
        var set = ids.ToHashSet();
        set.Add(me);
        return set;
    }

    private readonly record struct NodeRef(long Id, long? ParentId, StructureNodeType NodeType);

    private static long NearestDepartment(List<NodeRef> all, long start)
    {
        var byId = all.ToDictionary(n => n.Id);
        var current = byId.TryGetValue(start, out var node) ? (NodeRef?)node : null;
        while (current is { } c)
        {
            if (c.NodeType == StructureNodeType.Department) return c.Id;
            current = c.ParentId is { } pid && byId.TryGetValue(pid, out var parent) ? parent : null;
        }
        return start;
    }

    private static HashSet<long> Descendants(List<NodeRef> all, long root)
    {
        var childrenByParent = all.Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());
        var result = new HashSet<long> { root };
        var stack = new Stack<long>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var id = stack.Pop();
            if (!childrenByParent.TryGetValue(id, out var kids)) continue;
            foreach (var kid in kids.Where(result.Add)) stack.Push(kid);
        }
        return result;
    }
}
