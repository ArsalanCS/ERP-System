using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Structure;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.Structure;

public interface IStructureService
{
    Task<StructureTree> GetTreeAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<StructureMemberDto>>> ListMembersAsync(Guid nodeId, CancellationToken ct = default);
    Task<Result<Guid>> CreateNodeAsync(CreateNodeRequest request, CancellationToken ct = default);
    Task<Result> UpdateNodeAsync(Guid id, UpdateNodeRequest request, CancellationToken ct = default);
    Task<Result> MoveNodeAsync(Guid id, MoveNodeRequest request, CancellationToken ct = default);
    Task<Result> ArchiveNodeAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Business Structure (Identity spec §6) over a single self-nesting node tree.
/// Rules: Organization nodes are roots (no parent); every other node needs a
/// parent in the same workspace; codes are unique; a node with children is
/// archived only after its children; moves can't create cycles. All writes audited.
/// </summary>
public sealed class StructureService(
    IStructureRepository repo,
    IAuditLogger audit,
    IClock clock,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IStructureService
{
    public async Task<StructureTree> GetTreeAsync(CancellationToken ct = default)
    {
        var nodes = await repo.ListNodesAsync(ct);
        var counts = await repo.MemberCountsAsync(ct);
        var dtos = nodes
            .Select(n => new StructureNodeDto(n.Id, n.ParentId, n.NodeType, n.Name, n.Code, n.Description,
                n.ManagerId, n.SortOrder, n.Status, counts.GetValueOrDefault(n.Id)))
            .ToList();
        return new StructureTree(dtos);
    }

    public async Task<Result<IReadOnlyList<StructureMemberDto>>> ListMembersAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await repo.GetNodeAsync(nodeId, ct);
        if (node is null) return Result.Failure<IReadOnlyList<StructureMemberDto>>(StructureErrors.NotFound("Node"));

        var members = await repo.ListMembersAsync(nodeId, ct);
        // Flag the node's designated manager so the UI can highlight them.
        if (node.ManagerId is { } managerId)
        {
            members = members.Select(m => m.UserId == managerId ? m with { IsManager = true } : m).ToList();
        }
        return Result.Success(members);
    }

    public async Task<Result<Guid>> CreateNodeAsync(CreateNodeRequest request, CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } ws) return Result.Failure<Guid>(StructureErrors.NoScope());

        if (request.NodeType == StructureNodeType.Organization)
        {
            if (request.ParentId is not null) return Result.Failure<Guid>(StructureErrors.OrgMustBeRoot());
        }
        else
        {
            if (request.ParentId is not { } parentId)
                return Result.Failure<Guid>(StructureErrors.ParentRequired());
            if (!await repo.NodeExistsAsync(parentId, ct))
                return Result.Failure<Guid>(StructureErrors.NotFound("Parent node"));
        }

        if (await repo.CodeExistsAsync(request.Code.Trim(), ct))
            return Result.Failure<Guid>(StructureErrors.CodeTaken());

        var node = new StructureNode(ws, request.ParentId, request.NodeType, request.Name.Trim(), request.Code.Trim());
        node.Update(request.Name.Trim(), request.Description, request.ManagerId, request.SortOrder ?? 0);
        repo.AddNode(node);

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Create, Module = "BusinessStructure", ResourceType = "StructureNode",
            ResourceId = node.Id.ToString(), WorkspaceId = ws,
            NewValues = $"{{\"type\":\"{node.NodeType}\",\"code\":\"{node.Code}\"}}",
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return node.Id;
    }

    public async Task<Result> UpdateNodeAsync(Guid id, UpdateNodeRequest request, CancellationToken ct = default)
    {
        var node = await repo.GetNodeAsync(id, ct);
        if (node is null) return Result.Failure(StructureErrors.NotFound("Node"));

        node.Update(request.Name.Trim(), request.Description, request.ManagerId, request.SortOrder ?? node.SortOrder);
        return await CommitAsync(AuditActions.Update, node.Id, node.WorkspaceId, ct);
    }

    public async Task<Result> MoveNodeAsync(Guid id, MoveNodeRequest request, CancellationToken ct = default)
    {
        var node = await repo.GetNodeAsync(id, ct);
        if (node is null) return Result.Failure(StructureErrors.NotFound("Node"));

        if (node.NodeType == StructureNodeType.Organization)
            return Result.Failure(StructureErrors.OrgMustBeRoot());
        if (request.ParentId is not { } parentId)
            return Result.Failure(StructureErrors.ParentRequired());
        if (parentId == id)
            return Result.Failure(StructureErrors.SelfParent());
        if (!await repo.NodeExistsAsync(parentId, ct))
            return Result.Failure(StructureErrors.NotFound("Parent node"));

        // Reject cycles: the new parent must not be the node or one of its descendants.
        var all = await repo.ListNodesAsync(ct);
        if (DescendantIds(all, id).Contains(parentId))
            return Result.Failure(StructureErrors.Cycle());

        node.MoveTo(parentId);
        return await CommitAsync(AuditActions.Update, node.Id, node.WorkspaceId, ct);
    }

    public async Task<Result> ArchiveNodeAsync(Guid id, CancellationToken ct = default)
    {
        var node = await repo.GetNodeAsync(id, ct);
        if (node is null) return Result.Failure(StructureErrors.NotFound("Node"));
        if (await repo.NodeHasChildrenAsync(id, ct)) return Result.Failure(StructureErrors.HasChildren());

        node.Archive(null, clock.UtcNow);
        return await CommitAsync(AuditActions.Delete, node.Id, node.WorkspaceId, ct);
    }

    // ---- helpers -----------------------------------------------------------
    private static HashSet<Guid> DescendantIds(IReadOnlyList<StructureNode> all, Guid rootId)
    {
        var byParent = all.Where(n => n.ParentId is not null)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());
        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>([rootId]);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!byParent.TryGetValue(current, out var children)) continue;
            foreach (var child in children.Where(result.Add))
            {
                stack.Push(child);
            }
        }
        return result;
    }

    private async Task<Result> CommitAsync(string action, Guid id, Guid ws, CancellationToken ct)
    {
        await audit.LogAsync(new AuditEntry
        {
            Action = action, Module = "BusinessStructure", ResourceType = "StructureNode",
            ResourceId = id.ToString(), WorkspaceId = ws,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal static class StructureErrors
{
    public static Error NotFound(string what) => Error.NotFound($"{what} not found.");
    public static Error NoScope() => new("STR_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error SelfParent() => new("STR_SELF_PARENT", "A node cannot be its own parent.", ErrorType.Validation);
    public static Error Cycle() => new("STR_CYCLE", "A node cannot be moved under one of its own descendants.", ErrorType.Validation);
    public static Error OrgMustBeRoot() => new("STR_ORG_ROOT", "An organization must be a root node (no parent).", ErrorType.Validation);
    public static Error ParentRequired() => new("STR_PARENT_REQUIRED", "A parent node is required for this node type.", ErrorType.Validation);
    public static Error CodeTaken() => new("STR_CODE_TAKEN", "That code is already used in this workspace.", ErrorType.Conflict);
    public static Error HasChildren() => new("STR_HAS_CHILDREN", "Archive or move the child nodes first.", ErrorType.Conflict);
}
