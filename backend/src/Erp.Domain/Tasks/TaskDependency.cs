using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A scheduling dependency between two tasks (Task Model spec §7-8). Drives the
/// Gantt dependency arrows and "blocked until" semantics.
/// </summary>
public sealed class TaskDependency : TenantEntity
{
    private TaskDependency() { } // EF

    public TaskDependency(Guid workspaceId, Guid taskId, Guid dependsOnTaskId, TaskDependencyType type, bool isBlocking)
    {
        AssignWorkspace(workspaceId);
        TaskId = taskId;
        DependsOnTaskId = dependsOnTaskId;
        DependencyType = type;
        IsBlocking = isBlocking;
    }

    public Guid TaskId { get; private set; }
    public Guid DependsOnTaskId { get; private set; }
    public TaskDependencyType DependencyType { get; private set; }
    public bool IsBlocking { get; private set; }
}
