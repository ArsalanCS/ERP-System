using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using Erp.Domain.Structure;
using Erp.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Erp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Optimized Task Management read side over the <c>bpm.fn_task_*</c> Postgres
/// functions. Row Models are fetched via FromSqlRaw and mapped explicitly to DTOs
/// (company standard). DataScope is preserved by resolving the caller's visible
/// user-id set and passing it to every function.
/// </summary>
public sealed class TaskReadRepository(ErpDbContext db, IClock clock) : ITaskReadRepository
{
    public async Task<VisibleScope> GetVisibleScopeAsync(long workspaceId, long me, DataScope scope, CancellationToken ct = default)
    {
        if (scope >= DataScope.Cluster) return new VisibleScope(workspaceId, true, [], me);
        if (scope == DataScope.Own) return new VisibleScope(workspaceId, false, [me], me);

        var myNode = await db.Employees.Where(e => e.UserId == me)
            .Select(e => e.PlacementNodeId).FirstOrDefaultAsync(ct);
        if (myNode is not { } nodeId) return new VisibleScope(workspaceId, false, [me], me);

        var nodes = await db.StructureNodes.Select(n => new NodeRef(n.Id, n.ParentId, n.NodeType)).ToListAsync(ct);
        var root = scope == DataScope.Department ? NearestDepartment(nodes, nodeId) : nodeId;
        var subtree = Descendants(nodes, root);
        var ids = await db.Employees
            .Where(e => e.PlacementNodeId != null && subtree.Contains(e.PlacementNodeId.Value))
            .Select(e => e.UserId).ToListAsync(ct);
        var set = ids.ToHashSet();
        set.Add(me);
        return new VisibleScope(workspaceId, false, [.. set], me);
    }

    public async Task<TaskDashboardDto> GetDashboardAsync(VisibleScope scope, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var summary = (await db.Set<TaskSummaryRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_summary(@p_ws,@p_all,@p_users,@p_me,@p_now,@p_status,@p_priority,@p_overdue,@p_closed)",
                ScopeNowFilters(scope, now, null)).AsNoTracking().ToListAsync(ct)).Single();

        var byStatus = await BucketsAsync("bpm.fn_task_status_breakdown", scope, now, null, ct);
        var byPriority = await BucketsAsync("bpm.fn_task_priority_breakdown", scope, now, null, ct);
        var byAssignee = await AssigneesAsync(scope, now, null, ct);

        var trend = (await db.Set<TaskTrendRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_trend(@p_ws,@p_all,@p_users,@p_me,@p_from,@p_to)",
                [.. Scope(scope), Date("p_from", today.AddDays(-13)), Date("p_to", today)])
            .AsNoTracking().ToListAsync(ct))
            .Select(r => new TaskTrendPointDto(r.Day, r.Created, r.Completed)).ToList();

        var recent = (await db.Set<TaskRecentActivityRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_recent_activity(@p_ws,@p_all,@p_users,@p_me,@p_limit)",
                [.. Scope(scope), Int("p_limit", 15)]).AsNoTracking().ToListAsync(ct))
            .Select(r => new TaskRecentActivityDto(r.Id, r.EventId, r.ReferenceNo, r.Message, r.ActorId, r.ActorName, r.OccurredAt)).ToList();

        var gantt = (await db.Set<TaskGanttRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_gantt(@p_ws,@p_all,@p_users,@p_me,@p_limit)",
                [.. Scope(scope), Int("p_limit", 25)]).AsNoTracking().ToListAsync(ct))
            .Select(r => new TaskGanttItemDto(r.EventId, r.ReferenceNo, r.Title, r.StartAt, r.DueAt, r.CompletionPercent, r.StatusColor, r.IsClosed)).ToList();

        return new TaskDashboardDto(
            summary.Total, summary.Open, summary.InProgress, summary.Overdue, summary.DueToday,
            summary.DueThisWeek, summary.HighPriority, summary.Completed, summary.Unassigned,
            summary.CompletedLast7, summary.ReportsToday, summary.AvgCompletion,
            summary.EstimatedTotal, summary.ActualTotal,
            byStatus, byPriority, byAssignee.Take(10).ToList(), trend, recent, gantt);
    }

    public async Task<TaskReportDto> GetReportAsync(VisibleScope scope, TaskListQuery filters, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var summary = (await db.Set<TaskSummaryRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_summary(@p_ws,@p_all,@p_users,@p_me,@p_now,@p_status,@p_priority,@p_overdue,@p_closed)",
                ScopeNowFilters(scope, now, filters)).AsNoTracking().ToListAsync(ct)).Single();

        var byStatus = await BucketsAsync("bpm.fn_task_status_breakdown", scope, now, filters, ct);
        var byPriority = await BucketsAsync("bpm.fn_task_priority_breakdown", scope, now, filters, ct);
        var byAssignee = await AssigneesAsync(scope, now, filters, ct);

        return new TaskReportDto(summary.Total, summary.Open, summary.Completed, summary.Overdue,
            summary.EstimatedTotal, summary.ActualTotal, byStatus, byPriority, byAssignee);
    }

    public async Task<PagedResult<TaskDailyReportRowDto>> GetDailyReportsAsync(VisibleScope scope, TaskDailyReportQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize is < 1 or > 200 ? 25 : query.PageSize;

        var rows = await db.Set<TaskDailyReportRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_daily_reports(@p_ws,@p_all,@p_users,@p_me,@p_from,@p_to,@p_author,@p_status,@p_offset,@p_limit)",
                [.. Scope(scope),
                Date("p_from", query.FromDate), Date("p_to", query.ToDate),
                NullableLong("p_author", query.AuthorId), NullableLong("p_status", query.StatusId),
                Int("p_offset", (page - 1) * size), Int("p_limit", size)])
            .AsNoTracking().ToListAsync(ct);

        var total = rows.Count > 0 ? (int)rows[0].Total : 0;
        var items = rows.Select(r => new TaskDailyReportRowDto(
            r.Id, r.EventId, r.ReferenceNo, r.TaskTitle, r.ReportDate, r.Description,
            r.EstimatedTime, r.ActualTime, r.RemainingTime, r.StatusId, r.StatusName, r.StatusColor,
            r.AuthorId, r.AuthorName, r.CreatedAt)).ToList();
        return new PagedResult<TaskDailyReportRowDto>(items, page, size, total);
    }

    private async Task<IReadOnlyList<TaskBucketDto>> BucketsAsync(string fn, VisibleScope scope, DateTimeOffset now, TaskListQuery? filters, CancellationToken ct)
    {
        // fn is a fixed internal constant (status/priority breakdown); no user input.
        var sql = "SELECT * FROM " + fn + "(@p_ws,@p_all,@p_users,@p_me,@p_now,@p_status,@p_priority,@p_overdue,@p_closed)";
        return (await db.Set<TaskBucketRow>()
                .FromSqlRaw(sql, ScopeNowFilters(scope, now, filters)).AsNoTracking().ToListAsync(ct))
            .Select(r => new TaskBucketDto(r.Id, r.Name, r.Color, r.Count)).ToList();
    }

    private async Task<List<TaskAssigneeLoadDto>> AssigneesAsync(VisibleScope scope, DateTimeOffset now, TaskListQuery? filters, CancellationToken ct) =>
        (await db.Set<TaskAssigneeLoadRow>()
            .FromSqlRaw("SELECT * FROM bpm.fn_task_assignee_load(@p_ws,@p_all,@p_users,@p_me,@p_now,@p_status,@p_priority,@p_overdue,@p_closed)",
                ScopeNowFilters(scope, now, filters)).AsNoTracking().ToListAsync(ct))
        .Select(r => new TaskAssigneeLoadDto(r.AssigneeId, r.AssigneeName, r.Open, r.Overdue)).ToList();

    // ---- parameter helpers -------------------------------------------------
    private static NpgsqlParameter[] Scope(VisibleScope s) =>
    [
        new("p_ws", s.WorkspaceId),
        new("p_all", s.All),
        new("p_users", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = s.UserIds.ToArray() },
        new("p_me", s.Me),
    ];

    private static NpgsqlParameter[] ScopeNowFilters(VisibleScope s, DateTimeOffset now, TaskListQuery? f) =>
    [
        .. Scope(s),
        new("p_now", NpgsqlDbType.TimestampTz) { Value = now },
        NullableLong("p_status", f?.StatusId),
        NullableLong("p_priority", f?.PriorityStatusId),
        new("p_overdue", f?.Overdue ?? false),
        new("p_closed", f?.ClosedOnly ?? false),
    ];

    private static NpgsqlParameter NullableLong(string name, long? v) =>
        new(name, NpgsqlDbType.Bigint) { Value = (object?)v ?? DBNull.Value };

    private static NpgsqlParameter Date(string name, DateOnly? v) =>
        new(name, NpgsqlDbType.Date) { Value = (object?)v ?? DBNull.Value };

    private static NpgsqlParameter Int(string name, int v) =>
        new(name, NpgsqlDbType.Integer) { Value = v };

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
