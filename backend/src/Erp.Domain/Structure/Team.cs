using Erp.Domain.Common;

namespace Erp.Domain.Structure;

/// <summary>An operational team within a department (Identity spec §6.1).</summary>
public sealed class Team : TenantEntity
{
    private Team() { } // EF

    public Team(Guid workspaceId, Guid departmentId, string name, string code)
    {
        AssignWorkspace(workspaceId);
        DepartmentId = departmentId;
        Name = name;
        Code = code;
        Status = StructureStatus.Active;
    }

    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public Guid? LeadId { get; private set; }
    public StructureStatus Status { get; private set; }

    public void Update(string name, Guid? leadId)
    {
        Name = name;
        LeadId = leadId;
    }

    public void Archive(Guid? by, DateTimeOffset when)
    {
        Status = StructureStatus.Archived;
        SoftDelete(by, when);
    }
}
