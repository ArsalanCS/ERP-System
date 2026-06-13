using Erp.Domain.Common;

namespace Erp.Domain.Identity;

/// <summary>
/// Per-workspace security policy (Identity spec §7): password rules, lockout,
/// session lifetimes, and 2FA requirement. Exactly one row per workspace,
/// created on first read with safe defaults.
/// </summary>
public sealed class WorkspaceSecurityPolicy : TenantEntity
{
    private WorkspaceSecurityPolicy() { } // EF

    public WorkspaceSecurityPolicy(Guid workspaceId)
    {
        AssignWorkspace(workspaceId);
    }

    // ---- Password rules ----
    public int PasswordMinLength { get; private set; } = 8;
    public bool RequireUppercase { get; private set; } = true;
    public bool RequireLowercase { get; private set; } = true;
    public bool RequireDigit { get; private set; } = true;
    public bool RequireSymbol { get; private set; }
    /// <summary>Password maximum age in days; null = never expires.</summary>
    public int? PasswordExpiryDays { get; private set; }

    // ---- Lockout ----
    public int MaxFailedAttempts { get; private set; } = 5;
    public int LockoutMinutes { get; private set; } = 15;

    // ---- Sessions ----
    public int SessionIdleTimeoutMinutes { get; private set; } = 60;
    public int RefreshTokenDays { get; private set; } = 30;

    // ---- Two-factor ----
    /// <summary>When true, all members must enroll in 2FA.</summary>
    public bool RequireTwoFactor { get; private set; }

    public void Update(
        int passwordMinLength, bool requireUppercase, bool requireLowercase, bool requireDigit,
        bool requireSymbol, int? passwordExpiryDays, int maxFailedAttempts, int lockoutMinutes,
        int sessionIdleTimeoutMinutes, int refreshTokenDays, bool requireTwoFactor)
    {
        PasswordMinLength = passwordMinLength;
        RequireUppercase = requireUppercase;
        RequireLowercase = requireLowercase;
        RequireDigit = requireDigit;
        RequireSymbol = requireSymbol;
        PasswordExpiryDays = passwordExpiryDays;
        MaxFailedAttempts = maxFailedAttempts;
        LockoutMinutes = lockoutMinutes;
        SessionIdleTimeoutMinutes = sessionIdleTimeoutMinutes;
        RefreshTokenDays = refreshTokenDays;
        RequireTwoFactor = requireTwoFactor;
    }
}
