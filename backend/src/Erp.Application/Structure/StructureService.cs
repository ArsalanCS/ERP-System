using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Structure;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.Structure;

public interface IStructureService
{
    Task<StructureTree> GetTreeAsync(CancellationToken ct = default);

    Task<Result<Guid>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default);
    Task<Result> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);
    Task<Result> ArchiveOrganizationAsync(Guid id, CancellationToken ct = default);

    Task<Result<Guid>> CreateClusterAsync(CreateClusterRequest request, CancellationToken ct = default);
    Task<Result> UpdateClusterAsync(Guid id, UpdateClusterRequest request, CancellationToken ct = default);
    Task<Result> ArchiveClusterAsync(Guid id, CancellationToken ct = default);

    Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task<Result> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct = default);
    Task<Result> ArchiveDepartmentAsync(Guid id, CancellationToken ct = default);

    Task<Result<Guid>> CreateTeamAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<Result> UpdateTeamAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default);
    Task<Result> ArchiveTeamAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Business Structure (Identity spec §6). Structural rules: children can't cross
/// workspaces (enforced by tenant scope), and entities with dependents are
/// archived, not hard-deleted (§6.7). All writes audited.
/// </summary>
public sealed class StructureService(
    IStructureRepository repo,
    IAuditLogger audit,
    IClock clock,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IStructureService
{
    public async Task<StructureTree> GetTreeAsync(CancellationToken ct = default) => new(
        (await repo.ListOrganizationsAsync(ct)).Select(MapOrg).ToList(),
        (await repo.ListClustersAsync(ct)).Select(MapCluster).ToList(),
        (await repo.ListDepartmentsAsync(ct)).Select(MapDept).ToList(),
        (await repo.ListTeamsAsync(ct)).Select(MapTeam).ToList());

    // ---- Organizations -----------------------------------------------------
    public async Task<Result<Guid>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        if (Workspace() is not { } ws) return Result.Failure<Guid>(StructureErrors.NoScope());
        var org = new Organization(ws, request.Name, request.Code);
        org.Update(request.Name, request.LegalName, request.OrganizationType, request.CommercialRegistrationNumber,
            request.TaxNumber, request.Country, request.City, request.BaseCurrency ?? "SAR", request.ResponsibleManagerId);
        repo.AddOrganization(org);
        return await CommitCreateAsync(org.Id, ws, "Organization", ct);
    }

    public async Task<Result> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var org = await repo.GetOrganizationAsync(id, ct);
        if (org is null) return Result.Failure(StructureErrors.NotFound("Organization"));
        org.Update(request.Name, request.LegalName, request.OrganizationType, request.CommercialRegistrationNumber,
            request.TaxNumber, request.Country, request.City, request.BaseCurrency ?? "SAR", request.ResponsibleManagerId);
        return await CommitAsync(AuditActions.Update, id, org.WorkspaceId, "Organization", ct);
    }

    public async Task<Result> ArchiveOrganizationAsync(Guid id, CancellationToken ct = default)
    {
        var org = await repo.GetOrganizationAsync(id, ct);
        if (org is null) return Result.Failure(StructureErrors.NotFound("Organization"));
        org.Archive(tenant.WorkspaceId is null ? null : org.Id, clock.UtcNow);
        return await CommitAsync(AuditActions.Delete, id, org.WorkspaceId, "Organization", ct);
    }

    // ---- Clusters ----------------------------------------------------------
    public async Task<Result<Guid>> CreateClusterAsync(CreateClusterRequest request, CancellationToken ct = default)
    {
        if (Workspace() is not { } ws) return Result.Failure<Guid>(StructureErrors.NoScope());
        if (!await repo.OrganizationExistsAsync(request.OrganizationId, ct)) return Result.Failure<Guid>(StructureErrors.NotFound("Organization"));

        var cluster = new Cluster(ws, request.OrganizationId, request.Name, request.Code, request.Type);
        cluster.Update(request.Name, request.Type, request.Description, request.Location, request.ManagerId,
            request.ParentClusterId, request.DataIsolationEnabled, request.PermissionInheritanceEnabled);
        repo.AddCluster(cluster);
        return await CommitCreateAsync(cluster.Id, ws, "Cluster", ct);
    }

    public async Task<Result> UpdateClusterAsync(Guid id, UpdateClusterRequest request, CancellationToken ct = default)
    {
        var cluster = await repo.GetClusterAsync(id, ct);
        if (cluster is null) return Result.Failure(StructureErrors.NotFound("Cluster"));
        if (request.ParentClusterId == id) return Result.Failure(StructureErrors.SelfParent());
        cluster.Update(request.Name, request.Type, request.Description, request.Location, request.ManagerId,
            request.ParentClusterId, request.DataIsolationEnabled, request.PermissionInheritanceEnabled);
        return await CommitAsync(AuditActions.Update, id, cluster.WorkspaceId, "Cluster", ct);
    }

    public async Task<Result> ArchiveClusterAsync(Guid id, CancellationToken ct = default)
    {
        var cluster = await repo.GetClusterAsync(id, ct);
        if (cluster is null) return Result.Failure(StructureErrors.NotFound("Cluster"));
        cluster.Archive(null, clock.UtcNow);
        return await CommitAsync(AuditActions.Delete, id, cluster.WorkspaceId, "Cluster", ct);
    }

    // ---- Departments -------------------------------------------------------
    public async Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        if (Workspace() is not { } ws) return Result.Failure<Guid>(StructureErrors.NoScope());
        if (!await repo.OrganizationExistsAsync(request.OrganizationId, ct)) return Result.Failure<Guid>(StructureErrors.NotFound("Organization"));

        var dept = new Department(ws, request.OrganizationId, request.Name, request.Code);
        dept.Update(request.Name, request.ClusterId, request.ManagerId);
        repo.AddDepartment(dept);
        return await CommitCreateAsync(dept.Id, ws, "Department", ct);
    }

    public async Task<Result> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        var dept = await repo.GetDepartmentAsync(id, ct);
        if (dept is null) return Result.Failure(StructureErrors.NotFound("Department"));
        dept.Update(request.Name, request.ClusterId, request.ManagerId);
        return await CommitAsync(AuditActions.Update, id, dept.WorkspaceId, "Department", ct);
    }

    public async Task<Result> ArchiveDepartmentAsync(Guid id, CancellationToken ct = default)
    {
        var dept = await repo.GetDepartmentAsync(id, ct);
        if (dept is null) return Result.Failure(StructureErrors.NotFound("Department"));
        dept.Archive(null, clock.UtcNow);
        return await CommitAsync(AuditActions.Delete, id, dept.WorkspaceId, "Department", ct);
    }

    // ---- Teams -------------------------------------------------------------
    public async Task<Result<Guid>> CreateTeamAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        if (Workspace() is not { } ws) return Result.Failure<Guid>(StructureErrors.NoScope());
        if (!await repo.DepartmentExistsAsync(request.DepartmentId, ct)) return Result.Failure<Guid>(StructureErrors.NotFound("Department"));

        var team = new Team(ws, request.DepartmentId, request.Name, request.Code);
        team.Update(request.Name, request.LeadId);
        repo.AddTeam(team);
        return await CommitCreateAsync(team.Id, ws, "Team", ct);
    }

    public async Task<Result> UpdateTeamAsync(Guid id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await repo.GetTeamAsync(id, ct);
        if (team is null) return Result.Failure(StructureErrors.NotFound("Team"));
        team.Update(request.Name, request.LeadId);
        return await CommitAsync(AuditActions.Update, id, team.WorkspaceId, "Team", ct);
    }

    public async Task<Result> ArchiveTeamAsync(Guid id, CancellationToken ct = default)
    {
        var team = await repo.GetTeamAsync(id, ct);
        if (team is null) return Result.Failure(StructureErrors.NotFound("Team"));
        team.Archive(null, clock.UtcNow);
        return await CommitAsync(AuditActions.Delete, id, team.WorkspaceId, "Team", ct);
    }

    // ---- helpers -----------------------------------------------------------
    private Guid? Workspace() => tenant.WorkspaceId;

    private async Task<Result<Guid>> CommitCreateAsync(Guid id, Guid ws, string resource, CancellationToken ct)
    {
        await audit.LogAsync(new AuditEntry { Action = AuditActions.Create, Module = "BusinessStructure", ResourceType = resource, ResourceId = id.ToString(), WorkspaceId = ws }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return id;
    }

    private async Task<Result> CommitAsync(string action, Guid id, Guid ws, string resource, CancellationToken ct)
    {
        await audit.LogAsync(new AuditEntry { Action = action, Module = "BusinessStructure", ResourceType = resource, ResourceId = id.ToString(), WorkspaceId = ws }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static OrganizationDto MapOrg(Organization o) => new(o.Id, o.Name, o.Code, o.LegalName, o.OrganizationType, o.Country, o.City, o.BaseCurrency, o.Status);
    private static ClusterDto MapCluster(Cluster c) => new(c.Id, c.OrganizationId, c.ParentClusterId, c.Name, c.Code, c.Type, c.Location, c.ManagerId, c.DataIsolationEnabled, c.PermissionInheritanceEnabled, c.Status);
    private static DepartmentDto MapDept(Department d) => new(d.Id, d.OrganizationId, d.ClusterId, d.Name, d.Code, d.ManagerId, d.Status);
    private static TeamDto MapTeam(Team t) => new(t.Id, t.DepartmentId, t.Name, t.Code, t.LeadId, t.Status);
}

internal static class StructureErrors
{
    public static Error NotFound(string what) => Error.NotFound($"{what} not found.");
    public static Error NoScope() => new("STR_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error SelfParent() => new("STR_SELF_PARENT", "A cluster cannot be its own parent.", ErrorType.Validation);
}
