using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// A named task workflow set for a workspace (Task Model spec §4), e.g. "Default
/// Task Workflow" or "Sales Follow-up". Owns an ordered set of <see cref="TaskStatus"/>.
/// One workspace status type is flagged <see cref="IsDefault"/> and used when a
/// task is created without choosing a workflow.
/// </summary>
public sealed class TaskStatusType : TenantEntity
{
    private TaskStatusType() { } // EF

    public TaskStatusType(Guid workspaceId, string name)
    {
        AssignWorkspace(workspaceId);
        Name = name.Trim();
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public void Update(string name, string? description, int sortOrder)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
    public void SetActive(bool isActive) => IsActive = isActive;
}
