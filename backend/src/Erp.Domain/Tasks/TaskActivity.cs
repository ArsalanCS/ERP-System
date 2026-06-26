using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A user-visible activity entry for a task (the "Logs" tab; Task Model spec §5).
/// Status changes record <see cref="FromStatusId"/>/<see cref="ToStatusId"/>, which
/// doubles as the required status history. Distinct from the protected, append-only
/// audit trail (which reuses the existing <c>audit_logs</c>).
/// </summary>
public sealed class TaskActivity : TenantEntity
{
    private TaskActivity() { } // EF

    public TaskActivity(
        Guid workspaceId,
        Guid taskId,
        TaskActivityKind kind,
        string message,
        Guid? actorId,
        DateTimeOffset occurredAt,
        Guid? fromStatusId = null,
        Guid? toStatusId = null)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        Kind = kind;
        Message = message;
        ActorId = actorId;
        OccurredAt = occurredAt;
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
    }

    public Guid TaskId { get; private set; }
    public TaskActivityKind Kind { get; private set; }
    public string Message { get; private set; } = default!;
    public Guid? ActorId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? FromStatusId { get; private set; }
    public Guid? ToStatusId { get; private set; }
}
