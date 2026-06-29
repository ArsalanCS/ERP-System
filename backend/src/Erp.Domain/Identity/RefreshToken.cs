using Erp.Domain.Common;

namespace Erp.Domain.Identity;

/// <summary>
/// A rotating refresh token (CLAUDE.md §4.5). Only the SHA-256 hash of the raw
/// token is stored. On every refresh the token is rotated: the old one is
/// revoked and linked to its replacement. Presenting an already-rotated token is
/// treated as theft → revoke the whole chain.
/// </summary>
public sealed class RefreshToken : TenantEntity
{
    private RefreshToken() { } // EF

    public RefreshToken(long workspaceId, long userId, string tokenHash, DateTimeOffset expiresAt, string? createdByIp)
    {
        AssignWorkspace(workspaceId);
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public long? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsUsable(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }

    public void RotateTo(long replacementId, DateTimeOffset now)
    {
        Revoke(now);
        ReplacedByTokenId = replacementId;
    }
}
