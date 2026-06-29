using Erp.Api.Security;
using Erp.Application.Users;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Users management (Identity spec §4 / §13.1 /users). Every action declares its
/// required permission; writes are audited and tenant-scoped in the service.
/// </summary>
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(
    IUserService users,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<SuspendUserRequest> suspendValidator) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.UserView)]
    public async Task<IActionResult> List([FromQuery] UserListQuery query, CancellationToken ct)
        => FromResult(await users.ListAsync(query, ct), Ok);

    [HttpGet("{id:long}")]
    [RequirePermission(PermissionCatalog.UserView)]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
        => FromResult(await users.GetAsync(id, ct), Ok);

    [HttpPost]
    [RequirePermission(PermissionCatalog.UserManage)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, request, ct) is { } invalid) return invalid;
        var result = await users.CreateAsync(request, ct);
        return FromResult(result, r => CreatedAtAction(nameof(Get), new { id = r.UserId }, r));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(PermissionCatalog.UserManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await users.UpdateAsync(id, request, ct), NoContent);
    }

    [HttpPost("{id:long}/suspend")]
    [RequirePermission(PermissionCatalog.UserManage)]
    public async Task<IActionResult> Suspend(long id, [FromBody] SuspendUserRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(suspendValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await users.SuspendAsync(id, request, ct), NoContent);
    }

    [HttpPost("{id:long}/reactivate")]
    [RequirePermission(PermissionCatalog.UserManage)]
    public async Task<IActionResult> Reactivate(long id, CancellationToken ct)
        => FromResult(await users.ReactivateAsync(id, ct), NoContent);

    [HttpDelete("{id:long}")]
    [RequirePermission(PermissionCatalog.UserManage)]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
        => FromResult(await users.ArchiveAsync(id, ct), NoContent);
}
