using Erp.Application.Abstractions;
using Erp.Application.Structure;
using Erp.Domain.Structure;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class StructureRepository(ErpDbContext context) : IStructureRepository
{
    public async Task<IReadOnlyList<StructureNode>> ListNodesAsync(CancellationToken ct = default)
        => await context.StructureNodes.AsNoTracking()
            .OrderBy(n => n.NodeType).ThenBy(n => n.SortOrder).ThenBy(n => n.Name)
            .ToListAsync(ct);

    public Task<StructureNode?> GetNodeAsync(Guid id, CancellationToken ct = default)
        => context.StructureNodes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public void AddNode(StructureNode node) => context.StructureNodes.Add(node);

    public Task<bool> NodeExistsAsync(Guid id, CancellationToken ct = default)
        => context.StructureNodes.AnyAsync(n => n.Id == id, ct);

    public Task<bool> NodeHasChildrenAsync(Guid id, CancellationToken ct = default)
        => context.StructureNodes.AnyAsync(n => n.ParentId == id, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
        => context.StructureNodes.AnyAsync(n => n.Code == code, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> MemberCountsAsync(CancellationToken ct = default)
    {
        var counts = await context.Employees.AsNoTracking()
            .Where(e => e.PlacementNodeId != null)
            .GroupBy(e => e.PlacementNodeId!.Value)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        return counts.ToDictionary(x => x.NodeId, x => x.Count);
    }

    public async Task<IReadOnlyList<StructureMemberDto>> ListMembersAsync(Guid nodeId, CancellationToken ct = default)
    {
        var query =
            from e in context.Employees.AsNoTracking()
            where e.PlacementNodeId == nodeId
            join u in context.Users.AsNoTracking() on e.UserId equals u.Id
            orderby u.DisplayName
            select new StructureMemberDto(
                u.Id, u.DisplayName, u.Email, e.JobTitle, e.Mobile, e.EmployeeNumber, u.Status, false);
        return await query.ToListAsync(ct);
    }
}
