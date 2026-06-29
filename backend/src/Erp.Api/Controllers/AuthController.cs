using Erp.Api.Security;
using Erp.Application.Abstractions;
using Erp.Application.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Erp.Api.Controllers;

/// <summary>
/// Authentication endpoints (Identity spec §13.1 /auth). All are anonymous and
/// rate-limited (CLAUDE.md §4.5). Credential errors are deliberately generic.
/// </summary>
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class AuthController(
    IAuthService auth,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator,
    IValidator<ForgotPasswordRequest> forgotValidator,
    IValidator<ResetPasswordRequest> resetValidator,
    IValidator<RegisterWorkspaceRequest> registerValidator,
    IValidator<VerifyEmailRequest> verifyValidator,
    IValidator<ResendVerificationRequest> resendValidator) : ApiControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(loginValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.LoginAsync(request, ClientIp, ct);
        return FromResult(result, Ok);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokens), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(refreshValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.RefreshAsync(request, ClientIp, ct);
        return FromResult(result, Ok);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(logoutValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.LogoutAsync(request, ct);
        return FromResult(result, NoContent);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(forgotValidator, request, ct) is { } invalid) return invalid;

        // The service issues + emails the reset link when the account exists.
        // Always return 202 so the response never reveals whether it does.
        await auth.ForgotPasswordAsync(request, ct);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(resetValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.ResetPasswordAsync(request, ct);
        return FromResult(result, NoContent);
    }

    /// <summary>Self-service signup: create a workspace + owner, then email a verification link.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterWorkspaceResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterWorkspaceRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(registerValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.RegisterWorkspaceAsync(request, ct);
        return FromResult(result, value => StatusCode(StatusCodes.Status201Created, value));
    }

    /// <summary>Confirms an email-verification link and activates the owner account.</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(verifyValidator, request, ct) is { } invalid) return invalid;
        var result = await auth.VerifyEmailAsync(request, ct);
        return FromResult(result, NoContent);
    }

    /// <summary>Re-sends a verification link. Always returns 202 (no account enumeration).</summary>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(resendValidator, request, ct) is { } invalid) return invalid;
        await auth.ResendVerificationAsync(request, ct);
        return Accepted();
    }

    /// <summary>Returns the authenticated caller's identity. Requires a valid access token.</summary>
    [Authorize]
    [HttpGet("me")]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public IActionResult Me([FromServices] ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse(
            currentUser.UserId.Value,
            currentUser.WorkspaceId ?? 0,
            currentUser.Email ?? string.Empty,
            currentUser.IsPlatformAdmin,
            currentUser.Actions.ToArray()));
    }
}

public sealed record MeResponse(long Id, long WorkspaceId, string Email, bool IsPlatformAdmin, IReadOnlyCollection<string> Actions);
