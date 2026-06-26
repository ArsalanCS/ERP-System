using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A single status inside a <see cref="TaskStatusType"/> workflow (Task Model spec §4).
/// Maps to a reporting <see cref="TaskStatusCategory"/>; one status per type is the
/// <see cref="IsInitial"/> starting status, and <see cref="IsFinal"/> marks closed states
/// (Completed/Cancelled/Rejected) where editing is restricted.
/// </summary>
public sealed class TaskStatus : TenantEntity
{
    private TaskStatus() { } // EF

    public TaskStatus(Guid workspaceId, Guid statusTypeId, string name, TaskStatusCategory category)
    {
        AssignWorkspace(workspaceId);
        StatusTypeId = statusTypeId;
        Name = name.Trim();
        Category = category;
    }

    public Guid StatusTypeId { get; private set; }
    public string Name { get; private set; } = default!;
    public TaskStatusCategory Category { get; private set; }
    /// <summary>Optional UI accent (hex), e.g. "#3f7d52".</summary>
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsInitial { get; private set; }
    public bool IsFinal { get; private set; }

    public void Update(string name, TaskStatusCategory category, string? color, int sortOrder)
    {
        Name = name.Trim();
        Category = category;
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        SortOrder = sortOrder;
    }

    public void SetInitial(bool isInitial) => IsInitial = isInitial;
    public void SetFinal(bool isFinal) => IsFinal = isFinal;
}
