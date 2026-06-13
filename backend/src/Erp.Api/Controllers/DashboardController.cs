using Erp.Api.Security;
using Erp.Application.Dashboard;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Administration Dashboard (Identity spec §3): scope-aware KPI summary.</summary>
[Authorize]
[Route("api/v1/admin")]
public sealed class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    [HttpGet("overview")]
    [RequirePermission(PermissionCatalog.AdminOverviewView)]
    public async Task<IActionResult> Overview(CancellationToken ct) => Ok(await dashboard.GetSummaryAsync(ct));
}
