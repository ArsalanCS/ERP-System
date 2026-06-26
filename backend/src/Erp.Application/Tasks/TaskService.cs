using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Auditing;
using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Structure;
using Erp.Domain.Tasks;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Erp.Domain.Tasks.TaskStatus; // disambiguate from System.Threading.Tasks.TaskStatus

namespace Erp.Application.Tasks;

/// <summary>
/// Task Management (Task Model spec). Tasks are the only Event; subtasks are child
/// tasks; checklist/notes/documents/relations/dependencies are supporting records.
/// List/detail reads respect the caller's task.view <see cref="DataScope"/>
/// (own/team/department/broader). All writes are audited; status changes + key
/// edits are recorded as activity (Logs tab). Built on generic repositories.
/// </summary>
public sealed class TaskService(
    IRepository<TaskItem> tasks,
    IRepository<TaskStatus> statuses,
    IRepository<TaskStatusType> statusTypes,
    IRepository<TaskActivity> activities,
    IRepository<TaskChecklistItem> checklist,
    IRepository<TaskDependency> dependencies,
    IRepository<TaskRelation> relations,
    IRepository<User> users,
    IRepository<Employee> employees,
    IRepository<StructureNode> nodes,
    IRepository<AuditLog> auditLogs,
    IPermissionResolver permissions,
    ICurrentUser currentUser,
    IAuditLogger audit,
    IClock clock,
    IUnitOfWork unitOfWork) : ITaskService
{
    public async Task<Result<PagedResult<TaskListItemDto>>> ListAsync(TaskListQuery query, CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var visible = await VisibleAssigneeFilterAsync(ct);
        var now = clock.UtcNow;

        var q = tasks.Query();
        if (query.ParentTaskId is { } parentId) q = q.Where(t => t.ParentTaskId == parentId);
        else q = q.Where(t => t.ParentTaskId == null); // top-level tasks only in the main list
        q = ApplyVisibility(q, visible, me);
        if (query.StatusId is { } statusId) q = q.Where(t => t.StatusId == statusId);
        if (query.AssigneeId is { } assigneeId) q = q.Where(t => t.AssigneeId == assigneeId);
        if (query.Priority is { } priority) q = q.Where(t => t.Priority == priority);
        if (query.Overdue == true) q = q.Where(t => t.DueDate != null && t.DueDate < now);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(term) || t.TaskNumber.ToLower().Contains(term));
        }

        // Join into an anonymous shape so filter/sort run on real columns (EF can't
        // sort by a member of a constructed DTO). Project to the DTO last.
        var joined = from t in q
                     join s in statuses.Query() on t.StatusId equals s.Id
                     join au in users.Query() on t.AssigneeId equals au.Id into ag
                     from au in ag.DefaultIfEmpty()
                     select new { t, s, AssigneeName = au != null ? au.DisplayName : null };

        if (query.Category is { } category) joined = joined.Where(x => x.s.Category == category);

        joined = (query.Sort?.ToLowerInvariant()) switch
        {
            "title" => joined.OrderBy(x => x.t.Title),
            "-title" => joined.OrderByDescending(x => x.t.Title),
            "duedate" => joined.OrderBy(x => x.t.DueDate),
            "-duedate" => joined.OrderByDescending(x => x.t.DueDate),
            "priority" => joined.OrderByDescending(x => x.t.Priority),
            _ => joined.OrderByDescending(x => x.t.CreatedAt),
        };

        var projected = joined.Select(x => new TaskListItemDto(
            x.t.Id, x.t.TaskNumber, x.t.Title, x.t.Priority,
            x.s.Id, x.s.Name, x.s.Category, x.s.Color,
            x.t.AssigneeId, x.AssigneeName,
            x.t.DueDate, x.t.DueDate != null && x.t.DueDate < now && !x.s.IsFinal,
            x.t.CompletionPercent, x.t.CreatedAt));

        return Result.Success(await projected.ToPagedResultAsync(query.Page, query.PageSize, ct));
    }

    public async Task<Result<TaskDetailsDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var dto = await (
            from t in await VisibleTasksAsync(ct)
            where t.Id == id
            join s in statuses.Query() on t.StatusId equals s.Id
            join asg in users.Query() on t.AssigneeId equals asg.Id into ag
            from asg in ag.DefaultIfEmpty()
            join rep in users.Query() on t.ReporterId equals rep.Id into rg
            from rep in rg.DefaultIfEmpty()
            select new TaskDetailsDto(
                t.Id, t.TaskNumber, t.EventType, t.Title, t.Description,
                t.StatusTypeId, s.Id, s.Name, s.Category, s.Color, s.IsFinal,
                t.Priority,
                t.AssigneeId, asg != null ? asg.DisplayName : null,
                t.ReporterId, rep != null ? rep.DisplayName : null,
                t.ParentTaskId, t.SourceType, t.SourceId,
                t.StartDate, t.DueDate, t.EstimatedHours, t.ActualHours, t.ReminderAt,
                t.CompletionPercent,
                t.DueDate != null && t.DueDate < now && !s.IsFinal,
                t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return dto is null ? TaskErrors.NotFound("Task") : Result.Success(dto);
    }

    public async Task<Result<CreateTaskResult>> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
        => await CreateInternalAsync(request, parentId: null, ct);

    public async Task<Result<CreateTaskResult>> CreateSubtaskAsync(Guid parentId, CreateTaskRequest request, CancellationToken ct = default)
    {
        var parent = await GetVisibleAsync(parentId, ct);
        if (parent is null) return Result.Failure<CreateTaskResult>(TaskErrors.NotFound("Parent task"));
        if (await IsClosedAsync(parent.StatusId, ct)) return Result.Failure<CreateTaskResult>(TaskErrors.Closed());

        // A subtask inherits the parent's source by default (spec §6); an explicit source on the request overrides.
        if (string.IsNullOrWhiteSpace(request.SourceType) && parent.SourceType is not null)
            request = request with { SourceType = parent.SourceType, SourceId = parent.SourceId };

        return await CreateInternalAsync(request, parentId, ct);
    }

    private async Task<Result<CreateTaskResult>> CreateInternalAsync(CreateTaskRequest request, Guid? parentId, CancellationToken ct)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<CreateTaskResult>(TaskErrors.NoScope());

        var type = request.StatusTypeId is { } typeId
            ? await statusTypes.Query().FirstOrDefaultAsync(x => x.Id == typeId, ct)
            : await statusTypes.Query().Where(x => x.IsDefault && x.IsActive).OrderBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
        if (type is null) return Result.Failure<CreateTaskResult>(TaskErrors.NoWorkflow());

        var initial = await statuses.Query().Where(x => x.StatusTypeId == type.Id)
            .OrderByDescending(x => x.IsInitial).ThenBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
        if (initial is null) return Result.Failure<CreateTaskResult>(TaskErrors.NoInitialStatus());

        var number = await NextTaskNumberAsync(ws, ct);
        var task = new TaskItem(ws, number, request.Title, type.Id, initial.Id, currentUser.UserId);
        task.UpdateDetails(request.Title, request.Description, request.Priority);
        task.SetSchedule(request.StartDate, request.DueDate, request.EstimatedHours, request.ReminderAt);
        task.Assign(request.AssigneeId);
        task.SetSource(request.SourceType, request.SourceId);
        if (parentId is { } pid) task.PlaceUnderParent(pid);
        tasks.Add(task);

        AddActivity(ws, task.Id, parentId is null ? TaskActivityKind.Created : TaskActivityKind.SubtaskAdded, $"Task {number} created.");
        if (parentId is { } p) AddActivity(ws, p, TaskActivityKind.SubtaskAdded, $"Subtask {number} added.");
        await audit.LogAsync(Entry(AuditActions.Create, task.Id, ws, $"{{\"number\":\"{number}\"}}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateTaskResult(task.Id, number));
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(task.StatusId, ct)) return Result.Failure(TaskErrors.Closed());

        task.UpdateDetails(request.Title, request.Description, request.Priority);
        task.SetSchedule(request.StartDate, request.DueDate, request.EstimatedHours, request.ReminderAt);
        task.SetCompletion(request.CompletionPercent, request.ActualHours);

        AddActivity(task.WorkspaceId, task.Id, TaskActivityKind.Updated, "Task details updated.");
        await audit.LogAsync(Entry(AuditActions.Update, task.Id, task.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure(TaskErrors.NotFound("Task"));

        var status = await statuses.Query().FirstOrDefaultAsync(s => s.Id == request.StatusId, ct);
        if (status is null || status.StatusTypeId != task.StatusTypeId) return Result.Failure(TaskErrors.StatusNotInWorkflow());

        var fromStatusId = task.StatusId;
        task.ChangeStatus(status.Id);
        if (status.IsFinal && status.Category == TaskStatusCategory.Completed) task.SetCompletion(100, task.ActualHours);

        AddActivity(task.WorkspaceId, task.Id, TaskActivityKind.StatusChanged, $"Status changed to {status.Name}.", fromStatusId, status.Id);
        await audit.LogAsync(Entry(AuditActions.Update, task.Id, task.WorkspaceId, $"{{\"status\":\"{Escape(status.Name)}\"}}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure(TaskErrors.NotFound("Task"));
        if (request.AssigneeId is { } assigneeId && !await users.Query().AnyAsync(u => u.Id == assigneeId, ct))
            return Result.Failure(TaskErrors.NotFound("Assignee"));

        task.Assign(request.AssigneeId);
        AddActivity(task.WorkspaceId, task.Id, TaskActivityKind.Assigned, request.AssigneeId is null ? "Task unassigned." : "Task assigned.");
        await audit.LogAsync(Entry(AuditActions.Update, task.Id, task.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure(TaskErrors.NotFound("Task"));

        task.SoftDelete(currentUser.UserId, clock.UtcNow);
        AddActivity(task.WorkspaceId, task.Id, TaskActivityKind.Archived, "Task archived.");
        await audit.LogAsync(Entry(AuditActions.Delete, task.Id, task.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<TaskActivityDto>>> GetActivityAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskActivityDto>>(TaskErrors.NotFound("Task"));
        var items = await (
            from a in activities.Query()
            where a.TaskId == id
            join u in users.Query() on a.ActorId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby a.OccurredAt descending
            select new TaskActivityDto(a.Id, a.Kind, a.Message, a.ActorId, u != null ? u.DisplayName : null, a.OccurredAt))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskActivityDto>>(items);
    }

    public async Task<Result<IReadOnlyList<TaskAuditDto>>> GetAuditAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskAuditDto>>(TaskErrors.NotFound("Task"));
        var key = id.ToString();
        var items = await auditLogs.Query()
            .Where(a => a.ResourceType == "Task" && a.ResourceId == key)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new TaskAuditDto(a.Id, a.Action, a.ActorDisplayName, a.OccurredAt, a.Reason))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskAuditDto>>(items);
    }

    public async Task<Result<MyTasksGroups>> GetMyTasksAsync(CancellationToken ct = default)
    {
        var me = currentUser.UserId;
        var now = clock.UtcNow;
        var today = now.Date;
        var mine = Project(tasks.Query().Where(t => t.AssigneeId == me), now);
        var list = await mine.ToListAsync(ct);

        List<TaskListItemDto> Where(Func<TaskListItemDto, bool> p) => list.Where(p).ToList();
        var groups = new MyTasksGroups(
            Overdue: Where(x => x.DueDate != null && x.DueDate.Value.UtcDateTime.Date < today && x.StatusCategory is not (TaskStatusCategory.Completed or TaskStatusCategory.Cancelled or TaskStatusCategory.Rejected)),
            Today: Where(x => x.DueDate != null && x.DueDate.Value.UtcDateTime.Date == today),
            Upcoming: Where(x => x.DueDate != null && x.DueDate.Value.UtcDateTime.Date > today),
            Waiting: Where(x => x.StatusCategory == TaskStatusCategory.Waiting));
        return Result.Success(groups);
    }

    // ---- Subtasks ----------------------------------------------------------
    public async Task<Result<IReadOnlyList<TaskListItemDto>>> ListSubtasksAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskListItemDto>>(TaskErrors.NotFound("Task"));
        var items = await Project(tasks.Query().Where(t => t.ParentTaskId == id), clock.UtcNow).ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskListItemDto>>(items.OrderBy(x => x.CreatedAt).ToList());
    }

    // ---- Checklist ---------------------------------------------------------
    public async Task<Result<IReadOnlyList<ChecklistItemDto>>> ListChecklistAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<ChecklistItemDto>>(TaskErrors.NotFound("Task"));
        var items = await checklist.Query().Where(c => c.TaskId == id).OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt)
            .Select(c => new ChecklistItemDto(c.Id, c.Text, c.IsDone, c.SortOrder)).ToListAsync(ct);
        return Result.Success<IReadOnlyList<ChecklistItemDto>>(items);
    }

    public async Task<Result<Guid>> AddChecklistItemAsync(Guid id, CreateChecklistItemRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure<Guid>(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(task.StatusId, ct)) return Result.Failure<Guid>(TaskErrors.Closed());
        var count = await checklist.Query().CountAsync(c => c.TaskId == id, ct);
        var item = new TaskChecklistItem(task.WorkspaceId, id, request.Text, count);
        checklist.Add(item);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(item.Id);
    }

    public async Task<Result> UpdateChecklistItemAsync(Guid id, Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var item = await checklist.Query().FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == id, ct);
        if (item is null) return Result.Failure(TaskErrors.NotFound("Checklist item"));
        item.Update(request.Text, request.SortOrder);
        item.SetDone(request.IsDone);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveChecklistItemAsync(Guid id, Guid itemId, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var item = await checklist.Query().FirstOrDefaultAsync(c => c.Id == itemId && c.TaskId == id, ct);
        if (item is null) return Result.Failure(TaskErrors.NotFound("Checklist item"));
        checklist.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Dependencies ------------------------------------------------------
    public async Task<Result<IReadOnlyList<TaskDependencyDto>>> ListDependenciesAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskDependencyDto>>(TaskErrors.NotFound("Task"));
        var items = await (
            from d in dependencies.Query()
            where d.TaskId == id
            join t in tasks.Query() on d.DependsOnTaskId equals t.Id
            select new TaskDependencyDto(d.Id, d.DependsOnTaskId, t.TaskNumber, t.Title, d.DependencyType, d.IsBlocking))
            .ToListAsync(ct);
        return Result.Success<IReadOnlyList<TaskDependencyDto>>(items);
    }

    public async Task<Result<Guid>> AddDependencyAsync(Guid id, CreateDependencyRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure<Guid>(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(task.StatusId, ct)) return Result.Failure<Guid>(TaskErrors.Closed());
        if (request.DependsOnTaskId == id) return Result.Failure<Guid>(TaskErrors.SelfDependency());
        if (!await tasks.Query().AnyAsync(t => t.Id == request.DependsOnTaskId, ct)) return Result.Failure<Guid>(TaskErrors.NotFound("Dependency task"));
        if (await dependencies.Query().AnyAsync(d => d.TaskId == id && d.DependsOnTaskId == request.DependsOnTaskId, ct))
            return Result.Failure<Guid>(TaskErrors.DuplicateLink());

        var dep = new TaskDependency(task.WorkspaceId, id, request.DependsOnTaskId, request.DependencyType, request.IsBlocking);
        dependencies.Add(dep);
        AddActivity(task.WorkspaceId, id, TaskActivityKind.RelationChanged, "Dependency added.");
        await audit.LogAsync(Entry(AuditActions.Update, id, task.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(dep.Id);
    }

    public async Task<Result> RemoveDependencyAsync(Guid id, Guid dependencyId, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var dep = await dependencies.Query().FirstOrDefaultAsync(d => d.Id == dependencyId && d.TaskId == id, ct);
        if (dep is null) return Result.Failure(TaskErrors.NotFound("Dependency"));
        dependencies.Remove(dep);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Relations ---------------------------------------------------------
    public async Task<Result<IReadOnlyList<TaskRelationDto>>> ListRelationsAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskRelationDto>>(TaskErrors.NotFound("Task"));
        return Result.Success(await RelationsOf(id, ct));
    }

    public async Task<Result<Guid>> AddRelationAsync(Guid id, CreateRelationRequest request, CancellationToken ct = default)
    {
        var task = await GetVisibleAsync(id, ct);
        if (task is null) return Result.Failure<Guid>(TaskErrors.NotFound("Task"));
        if (await IsClosedAsync(task.StatusId, ct)) return Result.Failure<Guid>(TaskErrors.Closed());
        if (await relations.Query().AnyAsync(r => r.TaskId == id && r.RelatedEntityType == request.RelatedEntityType
            && r.RelatedEntityId == request.RelatedEntityId && r.Role == request.Role, ct))
            return Result.Failure<Guid>(TaskErrors.DuplicateLink());

        var rel = new TaskRelation(task.WorkspaceId, id, request.RelatedEntityType, request.RelatedEntityId, request.Role, request.Reason);
        relations.Add(rel);
        AddActivity(task.WorkspaceId, id, TaskActivityKind.RelationChanged, "Relation added.");
        await audit.LogAsync(Entry(AuditActions.Update, id, task.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(rel.Id);
    }

    public async Task<Result> RemoveRelationAsync(Guid id, Guid relationId, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure(TaskErrors.NotFound("Task"));
        var rel = await relations.Query().FirstOrDefaultAsync(r => r.Id == relationId && r.TaskId == id, ct);
        if (rel is null) return Result.Failure(TaskErrors.NotFound("Relation"));
        relations.Remove(rel);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Refresh Relations (Task Model spec §3). Idempotent — must not duplicate links.
    /// Auto-suggestion from the task's source record activates once the business
    /// modules (customers/invoices/assets…) exist; for now it returns current links.
    /// </summary>
    public async Task<Result<IReadOnlyList<TaskRelationDto>>> RefreshRelationsAsync(Guid id, CancellationToken ct = default)
    {
        if (await GetVisibleAsync(id, ct) is null) return Result.Failure<IReadOnlyList<TaskRelationDto>>(TaskErrors.NotFound("Task"));
        return Result.Success(await RelationsOf(id, ct));
    }

    private async Task<IReadOnlyList<TaskRelationDto>> RelationsOf(Guid id, CancellationToken ct)
        => await relations.Query().Where(r => r.TaskId == id)
            .Select(r => new TaskRelationDto(r.Id, r.RelatedEntityType, r.RelatedEntityId, r.Role, r.Reason)).ToListAsync(ct);

    // ---- visibility (DataScope) -------------------------------------------
    /// <summary>
    /// Returns null when the caller may see all workspace tasks (Cluster scope or
    /// broader), otherwise the set of assignee user-ids whose tasks are visible
    /// (own + reporter are always added at query time). Empty set = own/reporter only.
    /// </summary>
    private async Task<HashSet<Guid>?> VisibleAssigneeFilterAsync(CancellationToken ct)
    {
        if (currentUser.IsPlatformAdmin) return null;
        if (currentUser.UserId is not { } me) return new HashSet<Guid>();

        var perms = await permissions.ResolveAsync(me, ct);
        var scope = perms.ScopeFor(PermissionCatalog.TaskView) ?? DataScope.Own;
        if (scope >= DataScope.Cluster) return null; // Cluster/Organization/Workspace/AllTenants → all workspace tasks
        if (scope == DataScope.Own) return new HashSet<Guid>();

        var myNode = await employees.Query().Where(e => e.UserId == me).Select(e => e.PlacementNodeId).FirstOrDefaultAsync(ct);
        if (myNode is not { } placement) return new HashSet<Guid>();

        var all = await nodes.Query().Select(n => new NodeRef(n.Id, n.ParentId, n.NodeType)).ToListAsync(ct);
        var root = scope == DataScope.Department ? NearestDepartment(all, placement) : placement;
        var subtree = Descendants(all, root);
        var userIds = await employees.Query()
            .Where(e => e.PlacementNodeId != null && subtree.Contains(e.PlacementNodeId.Value))
            .Select(e => e.UserId).ToListAsync(ct);
        return userIds.ToHashSet();
    }

    private static IQueryable<TaskItem> ApplyVisibility(IQueryable<TaskItem> q, HashSet<Guid>? visible, Guid? me)
        => visible is null ? q
            : q.Where(t => t.AssigneeId == me || t.ReporterId == me || (t.AssigneeId != null && visible.Contains(t.AssigneeId.Value)));

    private async Task<IQueryable<TaskItem>> VisibleTasksAsync(CancellationToken ct)
        => ApplyVisibility(tasks.Query(), await VisibleAssigneeFilterAsync(ct), currentUser.UserId);

    private async Task<TaskItem?> GetVisibleAsync(Guid id, CancellationToken ct)
    {
        var task = await tasks.GetByIdAsync(id, ct);
        if (task is null) return null;
        var visible = await VisibleAssigneeFilterAsync(ct);
        if (visible is null) return task;
        var me = currentUser.UserId;
        var ok = task.AssigneeId == me || task.ReporterId == me || (task.AssigneeId is { } a && visible.Contains(a));
        return ok ? task : null;
    }

    /// <summary>
    /// Closed-status protection (Task Model spec §10): a task in a Final status
    /// (Done/Cancelled/Rejected) is locked against content edits and structural
    /// changes. Status changes are still allowed so the task can be reopened.
    /// </summary>
    private async Task<bool> IsClosedAsync(Guid statusId, CancellationToken ct)
        => await statuses.Query().AnyAsync(s => s.Id == statusId && s.IsFinal, ct);

    private readonly record struct NodeRef(Guid Id, Guid? ParentId, StructureNodeType NodeType);

    private static Guid NearestDepartment(List<NodeRef> all, Guid start)
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

    private static HashSet<Guid> Descendants(List<NodeRef> all, Guid root)
    {
        var children = all.Where(n => n.ParentId is not null).GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());
        var result = new HashSet<Guid> { root };
        var stack = new Stack<Guid>([root]);
        while (stack.Count > 0)
        {
            if (!children.TryGetValue(stack.Pop(), out var kids)) continue;
            foreach (var k in kids.Where(result.Add)) stack.Push(k);
        }
        return result;
    }

    // ---- helpers -----------------------------------------------------------
    private IQueryable<TaskListItemDto> Project(IQueryable<TaskItem> q, DateTimeOffset now)
        => from t in q
           join s in statuses.Query() on t.StatusId equals s.Id
           join au in users.Query() on t.AssigneeId equals au.Id into ag
           from au in ag.DefaultIfEmpty()
           select new TaskListItemDto(
               t.Id, t.TaskNumber, t.Title, t.Priority,
               s.Id, s.Name, s.Category, s.Color,
               t.AssigneeId, au != null ? au.DisplayName : null,
               t.DueDate, t.DueDate != null && t.DueDate < now && !s.IsFinal,
               t.CompletionPercent, t.CreatedAt);

    private async Task<string> NextTaskNumberAsync(Guid workspaceId, CancellationToken ct)
    {
        var seq = await tasks.Query().IgnoreQueryFilters().Where(t => t.WorkspaceId == workspaceId).CountAsync(ct) + 1;
        return $"TSK-{seq:D5}";
    }

    private void AddActivity(Guid ws, Guid taskId, TaskActivityKind kind, string message, Guid? fromStatusId = null, Guid? toStatusId = null)
        => activities.Add(new TaskActivity(ws, taskId, kind, message, currentUser.UserId, clock.UtcNow, fromStatusId, toStatusId));

    private static AuditEntry Entry(string action, Guid id, Guid ws, string? newValues = null) => new()
    {
        Action = action, Module = "Tasks", ResourceType = "Task", ResourceId = id.ToString(), WorkspaceId = ws, NewValues = newValues,
    };

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
