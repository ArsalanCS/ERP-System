using Erp.Shared.Errors;

namespace Erp.Application.Auth;

/// <summary>
/// Auth error factory. Credential failures are deliberately generic so the API
/// never reveals whether the email, workspace, or password was the problem.
/// </summary>
public static class AuthErrors
{
    public static Error InvalidCredentials() =>
        new("AUTH_INVALID_CREDENTIALS", "Invalid workspace, email, or password.", ErrorType.Unauthorized);

    public static Error AccountLocked() =>
        new("AUTH_ACCOUNT_LOCKED", "Account is temporarily locked due to failed login attempts.", ErrorType.Forbidden);

    public static Error AccountNotActive() =>
        new("AUTH_ACCOUNT_NOT_ACTIVE", "This account is not active. Contact your administrator.", ErrorType.Forbidden);

    public static Error InvalidRefreshToken() =>
        new("AUTH_INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.", ErrorType.Unauthorized);

    public static Error InvalidResetToken() =>
        new("AUTH_INVALID_RESET_TOKEN", "The reset link is invalid or has expired.", ErrorType.Validation);

    public static Error SlugTaken() =>
        new("AUTH_SLUG_TAKEN", "That workspace address is already taken. Please choose another.", ErrorType.Conflict);

    public static Error InvalidVerificationToken() =>
        new("AUTH_INVALID_VERIFICATION_TOKEN", "The verification link is invalid or has expired.", ErrorType.Validation);

    /// <summary>The account has 2FA enabled but no code was supplied — prompt for one.</summary>
    public static Error TwoFactorRequired() =>
        new("AUTH_2FA_REQUIRED", "A two-factor authentication code is required.", ErrorType.Unauthorized);

    public static Error InvalidTwoFactorCode() =>
        new("AUTH_INVALID_2FA_CODE", "The two-factor authentication code is invalid or has expired.", ErrorType.Unauthorized);
}
