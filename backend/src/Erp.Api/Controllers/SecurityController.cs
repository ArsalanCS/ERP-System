using Erp.Api.Security;
using Erp.Application.Security;
using Erp.Domain.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>Security policy (Identity spec §7): workspace password/lockout/session/2FA rules.</summary>
[Authorize]
[Route("api/v1/security")]
public sealed class SecurityController(
    ISecurityPolicyService policy,
    IValidator<UpdateSecurityPolicyRequest> updateValidator) : ApiControllerBase
{
    [HttpGet("policy")]
    [RequirePermission(PermissionCatalog.SecurityView)]
    public async Task<IActionResult> GetPolicy(CancellationToken ct)
        => FromResult(await policy.GetAsync(ct), Ok);

    [HttpPut("policy")]
    [RequirePermission(PermissionCatalog.SecurityManage)]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateSecurityPolicyRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await policy.UpdateAsync(request, ct), NoContent);
    }
}
