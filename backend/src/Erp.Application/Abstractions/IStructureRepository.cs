using Erp.Domain.Structure;

namespace Erp.Application.Abstractions;

/// <summary>Persistence for business-structure entities (orgs, clusters, departments, teams).</summary>
public interface IStructureRepository
{
    Task<IReadOnlyList<Organization>> ListOrganizationsAsync(CancellationToken ct = default);
    Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct = default);
    void AddOrganization(Organization organization);
    Task<bool> OrganizationHasChildrenAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Cluster>> ListClustersAsync(CancellationToken ct = default);
    Task<Cluster?> GetClusterAsync(Guid id, CancellationToken ct = default);
    void AddCluster(Cluster cluster);
    Task<bool> ClusterHasChildrenAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct = default);
    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken ct = default);
    void AddDepartment(Department department);
    Task<bool> DepartmentHasChildrenAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Team>> ListTeamsAsync(CancellationToken ct = default);
    Task<Team?> GetTeamAsync(Guid id, CancellationToken ct = default);
    void AddTeam(Team team);

    Task<bool> OrganizationExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> DepartmentExistsAsync(Guid id, CancellationToken ct = default);
}
