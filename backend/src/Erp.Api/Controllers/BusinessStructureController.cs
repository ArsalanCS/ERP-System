using Erp.Api.Security;
using Erp.Application.Structure;
using Erp.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Business Structure (Identity spec §6 / §13.1): organizations, clusters,
/// departments, teams. Reads require structure.view; writes require structure.manage.
/// </summary>
[Authorize]
[Route("api/v1")]
public sealed class BusinessStructureController(IStructureService structure) : ApiControllerBase
{
    [HttpGet("structure/tree")]
    [RequirePermission(PermissionCatalog.StructureView)]
    public async Task<IActionResult> Tree(CancellationToken ct) => Ok(await structure.GetTreeAsync(ct));

    // ---- Organizations ----
    [HttpPost("organizations")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> CreateOrg([FromBody] CreateOrganizationRequest req, CancellationToken ct)
        => FromResult(await structure.CreateOrganizationAsync(req, ct), id => Created($"/api/v1/organizations/{id}", new { id }));

    [HttpPut("organizations/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> UpdateOrg(Guid id, [FromBody] UpdateOrganizationRequest req, CancellationToken ct)
        => FromResult(await structure.UpdateOrganizationAsync(id, req, ct), NoContent);

    [HttpDelete("organizations/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> ArchiveOrg(Guid id, CancellationToken ct)
        => FromResult(await structure.ArchiveOrganizationAsync(id, ct), NoContent);

    // ---- Clusters ----
    [HttpPost("clusters")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> CreateCluster([FromBody] CreateClusterRequest req, CancellationToken ct)
        => FromResult(await structure.CreateClusterAsync(req, ct), id => Created($"/api/v1/clusters/{id}", new { id }));

    [HttpPut("clusters/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> UpdateCluster(Guid id, [FromBody] UpdateClusterRequest req, CancellationToken ct)
        => FromResult(await structure.UpdateClusterAsync(id, req, ct), NoContent);

    [HttpDelete("clusters/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> ArchiveCluster(Guid id, CancellationToken ct)
        => FromResult(await structure.ArchiveClusterAsync(id, ct), NoContent);

    // ---- Departments ----
    [HttpPost("departments")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> CreateDept([FromBody] CreateDepartmentRequest req, CancellationToken ct)
        => FromResult(await structure.CreateDepartmentAsync(req, ct), id => Created($"/api/v1/departments/{id}", new { id }));

    [HttpPut("departments/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> UpdateDept(Guid id, [FromBody] UpdateDepartmentRequest req, CancellationToken ct)
        => FromResult(await structure.UpdateDepartmentAsync(id, req, ct), NoContent);

    [HttpDelete("departments/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> ArchiveDept(Guid id, CancellationToken ct)
        => FromResult(await structure.ArchiveDepartmentAsync(id, ct), NoContent);

    // ---- Teams ----
    [HttpPost("teams")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest req, CancellationToken ct)
        => FromResult(await structure.CreateTeamAsync(req, ct), id => Created($"/api/v1/teams/{id}", new { id }));

    [HttpPut("teams/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] UpdateTeamRequest req, CancellationToken ct)
        => FromResult(await structure.UpdateTeamAsync(id, req, ct), NoContent);

    [HttpDelete("teams/{id:guid}")]
    [RequirePermission(PermissionCatalog.StructureManage)]
    public async Task<IActionResult> ArchiveTeam(Guid id, CancellationToken ct)
        => FromResult(await structure.ArchiveTeamAsync(id, ct), NoContent);
}
