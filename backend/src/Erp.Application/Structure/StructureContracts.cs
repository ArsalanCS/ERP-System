using Erp.Domain.Structure;

namespace Erp.Application.Structure;

public sealed record OrganizationDto(Guid Id, string Name, string Code, string? LegalName, string? OrganizationType,
    string? Country, string? City, string BaseCurrency, StructureStatus Status);
public sealed record CreateOrganizationRequest(string Name, string Code, string? LegalName, string? OrganizationType,
    string? CommercialRegistrationNumber, string? TaxNumber, string? Country, string? City, string? BaseCurrency, Guid? ResponsibleManagerId);
public sealed record UpdateOrganizationRequest(string Name, string? LegalName, string? OrganizationType,
    string? CommercialRegistrationNumber, string? TaxNumber, string? Country, string? City, string? BaseCurrency, Guid? ResponsibleManagerId);

public sealed record ClusterDto(Guid Id, Guid OrganizationId, Guid? ParentClusterId, string Name, string Code, string Type,
    string? Location, Guid? ManagerId, bool DataIsolationEnabled, bool PermissionInheritanceEnabled, StructureStatus Status);
public sealed record CreateClusterRequest(Guid OrganizationId, string Name, string Code, string Type, string? Description,
    string? Location, Guid? ManagerId, Guid? ParentClusterId, bool DataIsolationEnabled, bool PermissionInheritanceEnabled);
public sealed record UpdateClusterRequest(string Name, string Type, string? Description, string? Location, Guid? ManagerId,
    Guid? ParentClusterId, bool DataIsolationEnabled, bool PermissionInheritanceEnabled);

public sealed record DepartmentDto(Guid Id, Guid OrganizationId, Guid? ClusterId, string Name, string Code, Guid? ManagerId, StructureStatus Status);
public sealed record CreateDepartmentRequest(Guid OrganizationId, Guid? ClusterId, string Name, string Code, Guid? ManagerId);
public sealed record UpdateDepartmentRequest(string Name, Guid? ClusterId, Guid? ManagerId);

public sealed record TeamDto(Guid Id, Guid DepartmentId, string Name, string Code, Guid? LeadId, StructureStatus Status);
public sealed record CreateTeamRequest(Guid DepartmentId, string Name, string Code, Guid? LeadId);
public sealed record UpdateTeamRequest(string Name, Guid? LeadId);

/// <summary>The full structure tree for the workspace (Identity spec §6.2 split-screen tree).</summary>
public sealed record StructureTree(
    IReadOnlyList<OrganizationDto> Organizations,
    IReadOnlyList<ClusterDto> Clusters,
    IReadOnlyList<DepartmentDto> Departments,
    IReadOnlyList<TeamDto> Teams);
