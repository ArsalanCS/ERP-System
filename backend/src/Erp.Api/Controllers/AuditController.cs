using Erp.Api.Security;
using Erp.Application.Abstractions;
using Erp.Application.Auditing;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Audit &amp; Activity (Identity spec §8 / §13.1 /audit): search + export.</summary>
[Authorize]
[Route("api/v1/audit")]
public sealed class AuditController(IAuditQueryService audit, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.AuditView)]
    public async Task<IActionResult> Search([FromQuery] AuditSearchQuery query, CancellationToken ct)
        => Ok(await audit.SearchAsync(query, CanSeeSensitive(), ct));

    [HttpGet("export")]
    [RequirePermission(PermissionCatalog.AuditExport)]
    public async Task<IActionResult> Export([FromQuery] AuditSearchQuery query, CancellationToken ct)
        => Ok(await audit.ExportAsync(query, CanSeeSensitive(), ct));

    // Sensitive audit values are unmasked only for platform admins (extend with a
    // dedicated field-sensitivity permission later).
    private bool CanSeeSensitive() => currentUser.IsPlatformAdmin;
}
