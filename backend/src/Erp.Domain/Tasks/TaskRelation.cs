using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A link between a task and another business record (Task Model spec §8). Polymorphic
/// by <see cref="RelatedEntityType"/> + <see cref="RelatedEntityId"/> so a task can
/// reference records from modules that don't exist yet, without coupling. Never
/// crosses workspaces (rows are tenant-scoped).
/// </summary>
public sealed class TaskRelation : TenantEntity
{
    private TaskRelation() { } // EF

    public TaskRelation(Guid workspaceId, Guid taskId, string relatedEntityType, Guid relatedEntityId, TaskRelationRole role, string? reason)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        RelatedEntityType = relatedEntityType.Trim();
        RelatedEntityId = relatedEntityId;
        Role = role;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid TaskId { get; private set; }
    public string RelatedEntityType { get; private set; } = default!;
    public Guid RelatedEntityId { get; private set; }
    public TaskRelationRole Role { get; private set; }
    public string? Reason { get; private set; }
}
