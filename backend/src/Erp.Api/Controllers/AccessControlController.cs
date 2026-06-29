using Erp.Api.Security;
using Erp.Application.AccessControl;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Access Control (Identity spec §5 / §13.1 /roles): roles, permission matrix, overrides.</summary>
[Authorize]
[Route("api/v1")]
public sealed class AccessControlController(
    IRoleService roles,
    IValidator<CreateRoleRequest> createValidator,
    IValidator<UpdateRoleRequest> updateValidator) : ApiControllerBase
{
    [HttpGet("permissions")]
    [RequirePermission(PermissionCatalog.RoleView)]
    public async Task<IActionResult> ListPermissions(CancellationToken ct) => Ok(await roles.ListPermissionsAsync(ct));

    [HttpGet("roles")]
    [RequirePermission(PermissionCatalog.RoleView)]
    public async Task<IActionResult> ListRoles(CancellationToken ct) => Ok(await roles.ListRolesAsync(ct));

    [HttpGet("roles/{id:long}")]
    [RequirePermission(PermissionCatalog.RoleView)]
    public async Task<IActionResult> GetRole(long id, CancellationToken ct)
        => FromResult(await roles.GetRoleAsync(id, ct), Ok);

    [HttpPost("roles")]
    [RequirePermission(PermissionCatalog.RoleManage)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await roles.CreateRoleAsync(request, ct), id => CreatedAtAction(nameof(GetRole), new { id }, new { id }));
    }

    [HttpPut("roles/{id:long}")]
    [RequirePermission(PermissionCatalog.RoleManage)]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await roles.UpdateRoleAsync(id, request, ct), NoContent);
    }

    [HttpPut("roles/{id:long}/permissions")]
    [RequirePermission(PermissionCatalog.RoleManage)]
    public async Task<IActionResult> SetPermissions(long id, [FromBody] SetRolePermissionsRequest request, CancellationToken ct)
        => FromResult(await roles.SetPermissionsAsync(id, request, ct), NoContent);

    [HttpDelete("roles/{id:long}")]
    [RequirePermission(PermissionCatalog.RoleManage)]
    public async Task<IActionResult> DeleteRole(long id, CancellationToken ct)
        => FromResult(await roles.DeleteRoleAsync(id, ct), NoContent);

    [HttpGet("users/{userId:long}/overrides")]
    [RequirePermission(PermissionCatalog.RoleView)]
    public async Task<IActionResult> GetUserOverrides(long userId, CancellationToken ct)
        => FromResult(await roles.GetUserOverridesAsync(userId, ct), Ok);

    [HttpPut("users/{userId:long}/overrides")]
    [RequirePermission(PermissionCatalog.RoleManage)]
    public async Task<IActionResult> SetUserOverrides(long userId, [FromBody] SetUserOverridesRequest request, CancellationToken ct)
        => FromResult(await roles.SetUserOverridesAsync(userId, request, ct), NoContent);
}
