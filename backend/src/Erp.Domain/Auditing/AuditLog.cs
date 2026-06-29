using Erp.Domain.Common;

namespace Erp.Domain.Auditing;

/// <summary>
/// An immutable audit record (Identity spec §8.2, CLAUDE.md §4.3). Append-only:
/// the database blocks UPDATE/DELETE and the table is partitioned by month.
/// Tenant-owned (workspace_id) but never soft-deleted. Secrets must never be
/// written here — callers pass already-safe old/new value JSON.
/// </summary>
public sealed class AuditLog : BaseEntity, ITenantOwned
{
    private AuditLog() { } // EF

    public AuditLog(
        long workspaceId,
        string action,
        string module,
        string resourceType,
        AuditResult result,
        AuditSource source,
        DateTimeOffset occurredAt,
        string correlationId,
        long? actorUserId = null,
        string? actorDisplayName = null,
        long? organizationId = null,
        long? clusterId = null,
        string? resourceId = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? reason = null)
    {
        WorkspaceId = workspaceId;
        Action = action;
        Module = module;
        ResourceType = resourceType;
        Result = result;
        Source = source;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        ActorUserId = actorUserId;
        ActorDisplayName = actorDisplayName;
        OrganizationId = organizationId;
        ClusterId = clusterId;
        ResourceId = resourceId;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Reason = reason;
    }

    public long WorkspaceId { get; private set; }
    public long? OrganizationId { get; private set; }
    public long? ClusterId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
    public string CorrelationId { get; private set; } = null!;

    public long? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }

    public string Module { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public string? ResourceId { get; private set; }
    public string Action { get; private set; } = null!;

    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public AuditResult Result { get; private set; }
    public AuditSource Source { get; private set; }
    public string? Reason { get; private set; }
}
