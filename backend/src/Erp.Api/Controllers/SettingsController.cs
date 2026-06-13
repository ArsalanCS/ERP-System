using Erp.Api.Security;
using Erp.Application.Settings;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Workspace settings (Identity spec §9): general + localization.</summary>
[Authorize]
[Route("api/v1/settings")]
public sealed class SettingsController(ISettingsService settings) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.SettingsView)]
    public async Task<IActionResult> Get(CancellationToken ct) => FromResult(await settings.GetAsync(ct), Ok);

    [HttpPut]
    [RequirePermission(PermissionCatalog.SettingsManage)]
    public async Task<IActionResult> Update([FromBody] UpdateWorkspaceSettingsRequest request, CancellationToken ct)
        => FromResult(await settings.UpdateAsync(request, ct), NoContent);
}
