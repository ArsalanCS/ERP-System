using Erp.Domain.Common;

namespace Erp.Domain.Structure;

/// <summary>
/// A single node in the business-structure tree (Identity spec §6). One unified
/// self-nesting table models the whole hierarchy — organization, department,
/// branch, sub-department, team, sub-team — via <see cref="ParentId"/> +
/// <see cref="NodeType"/>. Members (employees) are placed on nodes separately.
/// </summary>
public sealed class StructureNode : TenantEntity
{
    private StructureNode() { } // EF

    public StructureNode(Guid workspaceId, Guid? parentId, StructureNodeType nodeType, string name, string code)
    {
        AssignWorkspace(workspaceId);
        ParentId = parentId;
        NodeType = nodeType;
        Name = name;
        Code = code;
        Status = StructureStatus.Active;
    }

    /// <summary>Parent node id; null only for root (Organization) nodes.</summary>
    public Guid? ParentId { get; private set; }

    public StructureNodeType NodeType { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }

    /// <summary>The user who manages/leads this node (head, manager, lead, …).</summary>
    public Guid? ManagerId { get; private set; }

    /// <summary>Ordering hint among siblings.</summary>
    public int SortOrder { get; private set; }

    public StructureStatus Status { get; private set; }

    public void Update(string name, string? description, Guid? managerId, int sortOrder)
    {
        Name = name;
        Description = description;
        ManagerId = managerId;
        SortOrder = sortOrder;
    }

    /// <summary>Re-parents the node (move within the tree). Caller guards against cycles.</summary>
    public void MoveTo(Guid? parentId) => ParentId = parentId;

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = StructureStatus.Archived;
        SoftDelete(by, when);
    }
}
