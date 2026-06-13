using Erp.Domain.Common;

namespace Erp.Domain.Structure;

/// <summary>A functional department within an organization/cluster (Identity spec §6.1).</summary>
public sealed class Department : TenantEntity
{
    private Department() { } // EF

    public Department(Guid workspaceId, Guid organizationId, string name, string code)
    {
        AssignWorkspace(workspaceId);
        OrganizationId = organizationId;
        Name = name;
        Code = code;
        Status = StructureStatus.Active;
    }

    public Guid OrganizationId { get; private set; }
    public Guid? ClusterId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public Guid? ManagerId { get; private set; }
    public StructureStatus Status { get; private set; }

    public void Update(string name, Guid? clusterId, Guid? managerId)
    {
        Name = name;
        ClusterId = clusterId;
        ManagerId = managerId;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = StructureStatus.Archived;
        SoftDelete(by, when);
    }
}
