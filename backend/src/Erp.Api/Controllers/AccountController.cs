using Erp.Application.Account;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

/// <summary>
/// Personal account (Identity spec §10). Any authenticated user manages their
/// own profile, password, and sessions — no admin permission required.
/// </summary>
[Authorize]
[Route("api/v1/me")]
public sealed class AccountController(
    IAccountService account,
    IValidator<UpdateMyProfileRequest> profileValidator,
    IValidator<ChangePasswordRequest> passwordValidator) : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => FromResult(await account.GetProfileAsync(ct), Ok);

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(profileValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await account.UpdateProfileAsync(request, ct), NoContent);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(passwordValidator, request, ct) is { } invalid) return invalid;
        return FromResult(await account.ChangePasswordAsync(request, ct), NoContent);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions(CancellationToken ct)
        => FromResult(await account.ListSessionsAsync(ct), Ok);

    [HttpDelete("sessions/{id:guid}")]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
        => FromResult(await account.RevokeSessionAsync(id, ct), NoContent);

    // ---- Two-factor (TOTP authenticator app, Identity spec §7) -------------

    /// <summary>Begins 2FA enrollment: returns the secret + otpauth URI to show as a QR code.</summary>
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken ct)
        => FromResult(await account.SetupTwoFactorAsync(ct), Ok);

    /// <summary>Confirms enrollment by verifying a code from the authenticator app.</summary>
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorCodeRequest request, CancellationToken ct)
        => FromResult(await account.EnableTwoFactorAsync(request, ct), NoContent);

    /// <summary>Disables 2FA after verifying a current authenticator code.</summary>
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorCodeRequest request, CancellationToken ct)
        => FromResult(await account.DisableTwoFactorAsync(request, ct), NoContent);
}
