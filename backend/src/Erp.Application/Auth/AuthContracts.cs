namespace Erp.Application.Auth;

// ---- Requests --------------------------------------------------------------

public sealed record LoginRequest(string WorkspaceSlug, string Email, string Password, string? TwoFactorCode = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string WorkspaceSlug, string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>Self-service signup: create a new workspace and its owner account (Identity spec §6.4).</summary>
public sealed record RegisterWorkspaceRequest(
    string WorkspaceName,
    string Slug,
    string BaseCurrency,
    string Language,
    string FullName,
    string Email,
    string Password);

public sealed record VerifyEmailRequest(string Token);

public sealed record ResendVerificationRequest(string WorkspaceSlug, string Email);

// ---- Results ---------------------------------------------------------------

public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUser User);

public sealed record AuthUser(
    long Id,
    long WorkspaceId,
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

/// <summary>
/// Result of a successful workspace registration. No tokens are issued — the
/// owner must verify their email (link sent by mail) before they can sign in.
/// </summary>
public sealed record RegisterWorkspaceResult(string Slug, string Email);
