using Erp.Api.Security;
using Erp.Application.Structure;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Business Structure (Identity spec §6 / §13.1): one unified node tree
/// (organization → department → branch → sub-department → team → sub-team).
/// Reads require structure.view; writes require structure.manage.
/// </summary>
[Authorize]
[Route("api/v1")]
public sealed class BusinessStructureController(IStructureService structure) : ApiControllerBase
{
    [HttpGet("structure/tree")]
    [RequirePermission(PermissionCatalog.StructureView)]
    public async Task<IActionResult> Tree(CancellationToken ct) => Ok(await structure.GetTreeAsync(ct));

    [HttpGet("structure/nodes/{id:long}/members")]
    [RequirePermission(PermissionCatalog.StructureView)]
    public async Task<IActionResult> Members(long id, CancellationToken ct)
        => FromResult(await structure.ListMembersAsync(id, ct), Ok);

    [HttpPost("structure/nodes")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> Create([FromBody] CreateNodeRequest req, CancellationToken ct)
        => FromResult(await structure.CreateNodeAsync(req, ct), id => Created($"/api/v1/structure/nodes/{id}", new { id }));

    [HttpPut("structure/nodes/{id:long}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateNodeRequest req, CancellationToken ct)
        => FromResult(await structure.UpdateNodeAsync(id, req, ct), NoContent);

    [HttpPut("structure/nodes/{id:long}/move")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> Move(long id, [FromBody] MoveNodeRequest req, CancellationToken ct)
        => FromResult(await structure.MoveNodeAsync(id, req, ct), NoContent);

    [HttpDelete("structure/nodes/{id:long}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
        => FromResult(await structure.ArchiveNodeAsync(id, ct), NoContent);
}
