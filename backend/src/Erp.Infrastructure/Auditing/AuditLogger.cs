using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Infrastructure.Persistence;
using Erp.Shared.Correlation;

namespace Erp.Infrastructure.Auditing;

/// <summary>
/// Default <see cref="IAuditLogger"/>: builds an <see cref="AuditLog"/> from the
/// entry + ambient context (actor, workspace, correlation, IP) and adds it to
/// the context. The audit row is written to the append-only, monthly-partitioned
/// <c>audit_logs</c> table (CLAUDE.md §4.3).
/// </summary>
public sealed class AuditLogger(
    ErpDbContext context,
    ICurrentUser currentUser,
    ITenantContext tenant,
    ICorrelationContext correlation,
    IClock clock) : IAuditLogger
{
    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        var workspaceId = entry.WorkspaceId ?? tenant.WorkspaceId ?? currentUser.WorkspaceId ?? Guid.Empty;

        var log = new AuditLog(
            workspaceId: workspaceId,
            action: entry.Action,
            module: entry.Module,
            resourceType: entry.ResourceType,
            result: entry.Result,
            source: entry.Source,
            occurredAt: clock.UtcNow,
            correlationId: correlation.CorrelationId,
            actorUserId: entry.ActorUserId ?? currentUser.UserId,
            actorDisplayName: entry.ActorDisplayName ?? currentUser.Email,
            organizationId: entry.OrganizationId,
            clusterId: entry.ClusterId,
            resourceId: entry.ResourceId,
            oldValues: entry.OldValues,
            newValues: entry.NewValues,
            ipAddress: null,
            userAgent: null,
            reason: entry.Reason);

        context.Set<AuditLog>().Add(log);
        return Task.CompletedTask;
    }
}
