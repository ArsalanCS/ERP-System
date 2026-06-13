using Erp.Shared.Results;

namespace Erp.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshAsync(RefreshRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
