using Erp.Shared.Results;

namespace Erp.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Self-service signup: provisions a workspace + owner and emails a verification link.</summary>
    Task<Result<RegisterWorkspaceResult>> RegisterWorkspaceAsync(RegisterWorkspaceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Activates a pending owner account from an email-verification token.</summary>
    Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>Re-sends the verification link. Always reports success (no account enumeration).</summary>
    Task<Result> ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default);
}
