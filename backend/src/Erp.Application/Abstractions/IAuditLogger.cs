using Erp.Domain.Auditing;

namespace Erp.Application.Abstractions;

/// <summary>
/// Writes immutable audit entries (CLAUDE.md §4.3). The entry is added to the
/// current unit of work; a SaveChanges within the same request persists it.
/// Never pass secrets in <see cref="AuditEntry.OldValues"/>/<see cref="AuditEntry.NewValues"/>.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Records an audit entry, filling actor/workspace/correlation from context when not supplied.</summary>
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>Data for a single audit entry. Context fields are optional overrides.</summary>
public sealed record AuditEntry
{
    public required string Action { get; init; }
    public required string Module { get; init; }
    public required string ResourceType { get; init; }
    public AuditResult Result { get; init; } = AuditResult.Success;
    public AuditSource Source { get; init; } = AuditSource.Api;

    public string? ResourceId { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? Reason { get; init; }

    // Optional overrides for flows where the HTTP principal isn't set yet (e.g. login).
    public long? WorkspaceId { get; init; }
    public long? ActorUserId { get; init; }
    public string? ActorDisplayName { get; init; }
    public long? OrganizationId { get; init; }
    public long? ClusterId { get; init; }
}
