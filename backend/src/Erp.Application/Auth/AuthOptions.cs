namespace Erp.Application.Auth;

/// <summary>
/// Auth policy knobs (CLAUDE.md §4.5). Bound from configuration; defaults match
/// the spec: 15-min access tokens, 30-day rolling refresh, lockout 5/15 min.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;

    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;

    public int PasswordResetTokenHours { get; set; } = 2;

    public string Issuer { get; set; } = "erp-platform";
    public string Audience { get; set; } = "erp-clients";
}
