using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task dashboard widgets (summary counts, gantt, recent activity, time analysis).
/// Heavy reads are served by the task read repository / DB functions.
/// </summary>
[Authorize]
[Route("api/v1/tasks/dashboard")]
public sealed class TaskDashboardController(ITaskService tasks) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
        => FromResult(await tasks.GetDashboardAsync(ct), Ok);
}
