using Erp.Application.Common;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;

namespace Erp.Application.Abstractions;

/// <summary>
/// The caller's visible task scope: the workspace, whether they see everything
/// (cluster+ DataScope), the set of user ids they may see, and their own id.
/// Computed once and passed to the report queries so DataScope is preserved.
/// </summary>
public readonly record struct VisibleScope(long WorkspaceId, bool All, IReadOnlyList<long> UserIds, long Me);

/// <summary>
/// Optimized read side for Task Management analytics (Refactor Guide §7: complex
/// reads use DB functions / a ReadRepository). Backed by the <c>bpm.fn_task_*</c>
/// Postgres functions whose Row Models are mapped explicitly to DTOs. Keeps EF/SQL
/// out of the application service.
/// </summary>
public interface ITaskReadRepository
{
    /// <summary>Resolves the visible user-id set for the caller's task.view DataScope.</summary>
    Task<VisibleScope> GetVisibleScopeAsync(long workspaceId, long me, DataScope scope, CancellationToken ct = default);

    /// <summary>Heavy paged task list (bpm.get_task_list), scoped to the caller's visible set.</summary>
    Task<PagedResult<TaskListItemDto>> ListAsync(VisibleScope scope, TaskListQuery query, CancellationToken ct = default);

    /// <summary>Single task details, returning null when not visible to the caller.</summary>
    Task<TaskDetailsDto?> GetDetailsAsync(VisibleScope scope, long eventId, CancellationToken ct = default);

    Task<TaskDashboardDto> GetDashboardAsync(VisibleScope scope, CancellationToken ct = default);

    Task<TaskReportDto> GetReportAsync(VisibleScope scope, TaskListQuery filters, CancellationToken ct = default);

    Task<PagedResult<TaskDailyReportRowDto>> GetDailyReportsAsync(VisibleScope scope, TaskDailyReportQuery query, CancellationToken ct = default);
}
