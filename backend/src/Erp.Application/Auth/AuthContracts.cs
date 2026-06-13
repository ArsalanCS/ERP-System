namespace Erp.Application.Auth;

// ---- Requests --------------------------------------------------------------

public sealed record LoginRequest(string WorkspaceSlug, string Email, string Password, string? TwoFactorCode = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string WorkspaceSlug, string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

// ---- Results ---------------------------------------------------------------

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUser User);

public sealed record AuthUser(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    string DisplayName,
    string PreferredLanguage,
    bool RequirePasswordChange,
    bool TwoFactorEnabled);

/// <summary>
/// Result of a forgot-password call. Always reported to the client as success
/// (no account enumeration); <see cref="RawToken"/> is for the mailer only.
/// </summary>
public sealed record ForgotPasswordResult(string? RawToken);
