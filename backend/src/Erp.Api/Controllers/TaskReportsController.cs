using Erp.Api.Security;
using Erp.Application.Tasks;
using Erp.Application.Tasks.Contracts;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Task reports (filtered task report + daily-report report). Backed by DB functions.
/// </summary>
[Authorize]
[Route("api/v1/tasks/report")]
public sealed class TaskReportsController(ITaskService tasks) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> Report([FromQuery] TaskListQuery query, CancellationToken ct)
        => FromResult(await tasks.GetReportAsync(query, ct), Ok);

    [HttpGet("daily-reports")]
    [RequirePermission(PermissionCatalog.TaskView)]
    public async Task<IActionResult> DailyReportsReport([FromQuery] TaskDailyReportQuery query, CancellationToken ct)
        => FromResult(await tasks.GetDailyReportsReportAsync(query, ct), Ok);
}
