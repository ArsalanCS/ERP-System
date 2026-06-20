using Erp.Application.Structure;
using Erp.Domain.Structure;

namespace Erp.Application.Abstractions;

/// <summary>Persistence for the unified business-structure node tree (Identity spec §6).</summary>
public interface IStructureRepository
{
    /// <summary>All active (non-archived) nodes for the workspace, ordered for tree building.</summary>
    Task<IReadOnlyList<StructureNode>> ListNodesAsync(CancellationToken ct = default);

    Task<StructureNode?> GetNodeAsync(Guid id, CancellationToken ct = default);
    void AddNode(StructureNode node);

    Task<bool> NodeExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> NodeHasChildrenAsync(Guid id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

    /// <summary>Number of employees placed directly on each node, keyed by node id.</summary>
    Task<IReadOnlyDictionary<Guid, int>> MemberCountsAsync(CancellationToken ct = default);

    /// <summary>Users placed directly on a node, joined with their employee details.</summary>
    Task<IReadOnlyList<StructureMemberDto>> ListMembersAsync(Guid nodeId, CancellationToken ct = default);
}
