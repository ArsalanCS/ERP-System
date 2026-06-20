using Erp.Domain.Identity;
using Erp.Domain.Structure;

namespace Erp.Application.Structure;

/// <summary>A single business-structure node (Identity spec §6).</summary>
public sealed record StructureNodeDto(
    Guid Id,
    Guid? ParentId,
    StructureNodeType NodeType,
    string Name,
    string Code,
    string? Description,
    Guid? ManagerId,
    int SortOrder,
    StructureStatus Status,
    int MemberCount);

/// <summary>The whole structure as a flat list; the client assembles the tree from ParentId.</summary>
public sealed record StructureTree(IReadOnlyList<StructureNodeDto> Nodes);

public sealed record CreateNodeRequest(
    Guid? ParentId,
    StructureNodeType NodeType,
    string Name,
    string Code,
    string? Description,
    Guid? ManagerId,
    int? SortOrder);

public sealed record UpdateNodeRequest(
    string Name,
    string? Description,
    Guid? ManagerId,
    int? SortOrder);

public sealed record MoveNodeRequest(Guid? ParentId);

/// <summary>A user placed directly on a structure node (Identity spec §6 "members").</summary>
public sealed record StructureMemberDto(
    Guid UserId,
    string DisplayName,
    string Email,
    string? JobTitle,
    string? Mobile,
    string? EmployeeNumber,
    UserStatus Status,
    bool IsManager);
