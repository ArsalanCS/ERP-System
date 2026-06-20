using Erp.Domain.Common;

namespace Erp.Domain.Identity;

/// <summary>
/// A user account. Per the chosen identity architecture, accounts are
/// <b>per-workspace</b>: email is unique within a workspace, and the same email
/// may exist as separate accounts in different workspaces. The account therefore
/// carries <c>workspace_id</c> directly (no separate membership row).
/// </summary>
public sealed class User : TenantEntity
{
    private User() { } // EF

    public User(Guid workspaceId, string email, string firstName, string lastName)
    {
        AssignWorkspace(workspaceId);
        SetEmail(email);
        FirstName = firstName;
        LastName = lastName;
        DisplayName = $"{firstName} {lastName}".Trim();
        Status = UserStatus.PendingInvitation;
        SecurityStamp = Guid.NewGuid().ToString("n");
    }

    // ---- Identity ----------------------------------------------------------
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;

    /// <summary>PBKDF2 hash (ASP.NET Identity hasher). Null until set (invited users).</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Rotated to invalidate all issued tokens/sessions (CLAUDE.md §4.5).</summary>
    public string SecurityStamp { get; private set; } = null!;

    public bool RequirePasswordChange { get; private set; }

    // ---- Profile -----------------------------------------------------------
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PreferredLanguage { get; private set; } = "en";
    public string TimeZone { get; private set; } = "Asia/Riyadh";
    public string? AvatarUrl { get; private set; }
    // Mobile, job title and other HR details live on the separate Employee record.

    // ---- Status & access window -------------------------------------------
    public UserStatus Status { get; private set; }
    public DateTimeOffset? AccessStartDate { get; private set; }
    public DateTimeOffset? AccessExpiryDate { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    // ---- Lockout (5 failed / 15 min — CLAUDE.md §4.5) ---------------------
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEndsAt { get; private set; }

    // ---- 2FA ---------------------------------------------------------------
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }

    public string FullName => DisplayName;

    public bool IsLockedOut(DateTimeOffset now) => LockoutEndsAt.HasValue && LockoutEndsAt.Value > now;

    /// <summary>Whether the account may currently authenticate.</summary>
    public bool CanLogin(DateTimeOffset now) =>
        Status == UserStatus.Active
        && !IsDeleted
        && !IsLockedOut(now)
        && (AccessStartDate is null || AccessStartDate <= now)
        && (AccessExpiryDate is null || AccessExpiryDate >= now);

    public void SetEmail(string email)
    {
        Email = email.Trim();
        NormalizedEmail = Email.ToUpperInvariant();
    }

    public void SetPasswordHash(string hash, bool requireChange = false)
    {
        PasswordHash = hash;
        RequirePasswordChange = requireChange;
        RotateSecurityStamp();
    }

    public void UpdateProfile(string firstName, string lastName, string displayName,
        string preferredLanguage, string timeZone)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"{firstName} {lastName}".Trim() : displayName;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
    }

    public void SetAccessWindow(DateTimeOffset? start, DateTimeOffset? expiry)
    {
        AccessStartDate = start;
        AccessExpiryDate = expiry;
    }

    /// <summary>Activates a pending/invited account (e.g. after accepting the invite).</summary>
    public void Activate() => Status = UserStatus.Active;

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        LastLoginAt = now;
        AccessFailedCount = 0;
        LockoutEndsAt = null;
    }

    /// <summary>Records a failed attempt; locks the account when the threshold is hit.</summary>
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration, DateTimeOffset now)
    {
        AccessFailedCount++;
        if (AccessFailedCount >= maxAttempts)
        {
            LockoutEndsAt = now.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    /// <summary>Suspends access and rotates the security stamp to revoke tokens/sessions (spec §4.6).</summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
        RotateSecurityStamp();
    }

    public void Reactivate()
    {
        Status = UserStatus.Active;
        LockoutEndsAt = null;
        AccessFailedCount = 0;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = UserStatus.Archived;
        RotateSecurityStamp();
        SoftDelete(by, when);
    }

    /// <summary>
    /// Stores a pending authenticator secret without enabling 2FA yet. The user must
    /// confirm with a valid code (<see cref="ConfirmTwoFactor"/>) before it takes effect.
    /// </summary>
    public void BeginTwoFactorEnrollment(string secret)
    {
        TwoFactorSecret = secret;
        TwoFactorEnabled = false;
    }

    /// <summary>Activates 2FA after the pending secret has been verified.</summary>
    public void ConfirmTwoFactor()
    {
        if (string.IsNullOrEmpty(TwoFactorSecret))
        {
            throw new InvalidOperationException("No pending two-factor secret to confirm.");
        }
        TwoFactorEnabled = true;
        RotateSecurityStamp();
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        RotateSecurityStamp();
    }

    public void RotateSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("n");
}
