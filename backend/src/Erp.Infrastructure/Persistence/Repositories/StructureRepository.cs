using Erp.Application.Abstractions;
using Erp.Domain.Structure;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class StructureRepository(ErpDbContext context) : IStructureRepository
{
    public async Task<IReadOnlyList<Organization>> ListOrganizationsAsync(CancellationToken ct = default)
        => await context.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync(ct);

    public Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken ct = default)
        => context.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public void AddOrganization(Organization organization) => context.Organizations.Add(organization);

    public Task<bool> OrganizationHasChildrenAsync(Guid id, CancellationToken ct = default)
        => context.Clusters.AnyAsync(c => c.OrganizationId == id, ct);

    public async Task<IReadOnlyList<Cluster>> ListClustersAsync(CancellationToken ct = default)
        => await context.Clusters.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Cluster?> GetClusterAsync(Guid id, CancellationToken ct = default)
        => context.Clusters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void AddCluster(Cluster cluster) => context.Clusters.Add(cluster);

    public Task<bool> ClusterHasChildrenAsync(Guid id, CancellationToken ct = default)
        => context.Clusters.AnyAsync(c => c.ParentClusterId == id, ct);

    public async Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct = default)
        => await context.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken ct = default)
        => context.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public void AddDepartment(Department department) => context.Departments.Add(department);

    public Task<bool> DepartmentHasChildrenAsync(Guid id, CancellationToken ct = default)
        => context.Teams.AnyAsync(t => t.DepartmentId == id, ct);

    public async Task<IReadOnlyList<Team>> ListTeamsAsync(CancellationToken ct = default)
        => await context.Teams.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public Task<Team?> GetTeamAsync(Guid id, CancellationToken ct = default)
        => context.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);

    public void AddTeam(Team team) => context.Teams.Add(team);

    public Task<bool> OrganizationExistsAsync(Guid id, CancellationToken ct = default)
        => context.Organizations.AnyAsync(o => o.Id == id, ct);

    public Task<bool> DepartmentExistsAsync(Guid id, CancellationToken ct = default)
        => context.Departments.AnyAsync(d => d.Id == id, ct);
}
