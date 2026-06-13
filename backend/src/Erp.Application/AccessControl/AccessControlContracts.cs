using Erp.Domain.Authorization;

namespace Erp.Application.AccessControl;

public sealed record PermissionDto(Guid Id, string Code, string Module, string Resource, string Action, bool IsHighRisk);

public sealed record RolePermissionDto(Guid PermissionId, string Code, DataScope Scope);

public sealed record RoleListItem(Guid Id, string Name, string Code, RoleType Type, string? Color, bool IsActive, int AssignedUsers);

public sealed record RoleDetail(
    Guid Id, string Name, string Code, string? Description, RoleType Type, string? Color, bool IsActive,
    IReadOnlyList<RolePermissionDto> Permissions);

public sealed record CreateRoleRequest(string Name, string Code, string? Description, string? Color);

public sealed record UpdateRoleRequest(string Name, string? Description, string? Color);

public sealed record PermissionGrant(Guid PermissionId, DataScope Scope);

public sealed record SetRolePermissionsRequest(IReadOnlyList<PermissionGrant> Permissions);

public sealed record UserOverrideDto(Guid PermissionId, string Code, PermissionEffect Effect, DataScope Scope);

public sealed record SetUserOverridesRequest(IReadOnlyList<UserOverrideInput> Overrides);

public sealed record UserOverrideInput(Guid PermissionId, PermissionEffect Effect, DataScope Scope);
