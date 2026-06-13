using Erp.Domain.Common;

namespace Erp.Domain.Structure;

/// <summary>
/// A configurable access/segmentation boundary — branch, subsidiary, region,
/// business unit, etc. (Identity spec §6.3/§6.6). The cluster id is what RBAC
/// scopes per-user roles to.
/// </summary>
public sealed class Cluster : TenantEntity
{
    private Cluster() { } // EF

    public Cluster(Guid workspaceId, Guid organizationId, string name, string code, string type)
    {
        AssignWorkspace(workspaceId);
        OrganizationId = organizationId;
        Name = name;
        Code = code;
        Type = type;
        Status = StructureStatus.Active;
    }

    public Guid OrganizationId { get; private set; }
    public Guid? ParentClusterId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;

    /// <summary>Configurable cluster type (branch, region, unit, …) — spec §6.3.</summary>
    public string Type { get; private set; } = null!;

    public string? Description { get; private set; }
    public string? Location { get; private set; }
    public Guid? ManagerId { get; private set; }
    public bool DataIsolationEnabled { get; private set; } = true;
    public bool PermissionInheritanceEnabled { get; private set; } = true;
    public StructureStatus Status { get; private set; }

    public void Update(string name, string type, string? description, string? location, Guid? managerId,
        Guid? parentClusterId, bool dataIsolation, bool permissionInheritance)
    {
        Name = name;
        Type = type;
        Description = description;
        Location = location;
        ManagerId = managerId;
        ParentClusterId = parentClusterId;
        DataIsolationEnabled = dataIsolation;
        PermissionInheritanceEnabled = permissionInheritance;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = StructureStatus.Archived;
        SoftDelete(by, when);
    }
}
