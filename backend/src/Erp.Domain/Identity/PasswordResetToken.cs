using Erp.Domain.Common;

namespace Erp.Domain.Identity;

/// <summary>
/// A single-use password-reset token. Only the hash is stored; the raw token is
/// emailed to the user. Consuming it resets the password and revokes sessions
/// (Identity spec §11 Password Reset workflow).
/// </summary>
public sealed class PasswordResetToken : TenantEntity
{
    private PasswordResetToken() { } // EF

    public PasswordResetToken(long workspaceId, long userId, string tokenHash, DateTimeOffset expiresAt)
    {
        AssignWorkspace(workspaceId);
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;

    public void Consume(DateTimeOffset now) => UsedAt = now;
}
