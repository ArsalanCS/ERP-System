using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Erp.Domain.Authorization;
using Erp.Shared.Errors;
using Erp.Shared.Results;

namespace Erp.Application.AccessControl;

public interface IRoleService
{
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleListItem>> ListRolesAsync(CancellationToken ct = default);
    Task<Result<RoleDetail>> GetRoleAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<Result> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);
    Task<Result> SetPermissionsAsync(Guid id, SetRolePermissionsRequest request, CancellationToken ct = default);
    Task<Result> DeleteRoleAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserOverrideDto>>> GetUserOverridesAsync(Guid userId, CancellationToken ct = default);
    Task<Result> SetUserOverridesAsync(Guid userId, SetUserOverridesRequest request, CancellationToken ct = default);
}

/// <summary>Access Control (Identity spec §5): roles, permission matrix, user overrides.</summary>
public sealed class RoleService(
    IRoleRepository roles,
    IPermissionRepository permissions,
    IUserRepository users,
    IAuditLogger audit,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IRoleService
{
    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default)
        => (await permissions.ListAsync(ct))
            .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Resource, p.Action, p.IsHighRisk)).ToList();

    public async Task<IReadOnlyList<RoleListItem>> ListRolesAsync(CancellationToken ct = default)
    {
        var list = await roles.ListAsync(ct);
        var result = new List<RoleListItem>(list.Count);
        foreach (var r in list)
        {
            result.Add(new RoleListItem(r.Id, r.Name, r.Code, r.Type, r.Color, r.IsActive, await roles.CountAssignmentsAsync(r.Id, ct)));
        }
        return result;
    }

    public async Task<Result<RoleDetail>> GetRoleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return AccessErrors.RoleNotFound();
        var catalog = (await permissions.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Code);
        var grants = await roles.GetRolePermissionsAsync(role.Id, ct);
        var perms = grants
            .Select(g => new RolePermissionDto(g.PermissionId, catalog.GetValueOrDefault(g.PermissionId, ""), g.Scope))
            .ToList();
        return new RoleDetail(role.Id, role.Name, role.Code, role.Description, role.Type, role.Color, role.IsActive, perms);
    }

    public async Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure<Guid>(AccessErrors.NoScope());
        if (await roles.CodeExistsAsync(request.Code, ct)) return Result.Failure<Guid>(AccessErrors.CodeTaken());

        var role = new Role(workspaceId, request.Name, request.Code, RoleType.Custom, request.Description);
        role.Update(request.Name, request.Description, request.Color);
        roles.Add(role);

        await audit.LogAsync(RoleAudit(AuditActions.Create, role.Id, workspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return role.Id;
    }

    public async Task<Result> UpdateRoleAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return Result.Failure(AccessErrors.RoleNotFound());
        if (role.IsSystem) return Result.Failure(AccessErrors.SystemRoleImmutable());

        role.Update(request.Name, request.Description, request.Color);
        await audit.LogAsync(RoleAudit(AuditActions.Update, role.Id, role.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetPermissionsAsync(Guid id, SetRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return Result.Failure(AccessErrors.RoleNotFound());
        if (role.IsSystem) return Result.Failure(AccessErrors.SystemRoleImmutable());

        var ids = request.Permissions.Select(p => p.PermissionId).ToList();
        var existing = await permissions.FilterExistingIdsAsync(ids, ct);
        if (existing.Count != ids.Distinct().Count()) return Result.Failure(AccessErrors.UnknownPermission());

        var grants = request.Permissions.Select(p => new PermissionGrantData(p.PermissionId, p.Scope)).ToList();
        await roles.ReplaceRolePermissionsAsync(role.WorkspaceId, role.Id, grants, ct);

        await audit.LogAsync(RoleAudit(AuditActions.PermissionChange, role.Id, role.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteRoleAsync(Guid id, CancellationToken ct = default)
    {
        var role = await roles.GetByIdAsync(id, ct);
        if (role is null) return Result.Failure(AccessErrors.RoleNotFound());
        if (role.IsSystem) return Result.Failure(AccessErrors.SystemRoleImmutable());
        if (await roles.CountAssignmentsAsync(role.Id, ct) > 0) return Result.Failure(AccessErrors.RoleInUse());

        roles.Remove(role);
        await audit.LogAsync(RoleAudit(AuditActions.Delete, role.Id, role.WorkspaceId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<UserOverrideDto>>> GetUserOverridesAsync(Guid userId, CancellationToken ct = default)
    {
        if (await users.GetByIdAsync(userId, ct) is null) return Result.Failure<IReadOnlyList<UserOverrideDto>>(AccessErrors.UserNotFound());
        var catalog = (await permissions.ListAsync(ct)).ToDictionary(p => p.Id, p => p.Code);
        var overrides = await permissions.ListUserOverridesAsync(userId, ct);
        IReadOnlyList<UserOverrideDto> result = overrides
            .Select(o => new UserOverrideDto(o.PermissionId, catalog.GetValueOrDefault(o.PermissionId, ""), o.Effect, o.Scope))
            .ToList();
        return Result.Success(result);
    }

    public async Task<Result> SetUserOverridesAsync(Guid userId, SetUserOverridesRequest request, CancellationToken ct = default)
    {
        if (tenant.WorkspaceId is not { } workspaceId) return Result.Failure(AccessErrors.NoScope());
        if (await users.GetByIdAsync(userId, ct) is null) return Result.Failure(AccessErrors.UserNotFound());

        var ids = request.Overrides.Select(o => o.PermissionId).ToList();
        var existing = await permissions.FilterExistingIdsAsync(ids, ct);
        if (existing.Count != ids.Distinct().Count()) return Result.Failure(AccessErrors.UnknownPermission());

        await permissions.RemoveUserOverridesAsync(userId, ct);
        foreach (var o in request.Overrides)
        {
            permissions.AddUserOverride(new UserPermission(workspaceId, userId, o.PermissionId, o.Effect, o.Scope));
        }

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.PermissionChange, Module = "AccessControl", ResourceType = "UserPermission",
            ResourceId = userId.ToString(), WorkspaceId = workspaceId,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static AuditEntry RoleAudit(string action, Guid roleId, Guid workspaceId) => new()
    {
        Action = action, Module = "AccessControl", ResourceType = "Role",
        ResourceId = roleId.ToString(), WorkspaceId = workspaceId,
    };
}

internal static class AccessErrors
{
    public static Error RoleNotFound() => Error.NotFound("Role not found.");
    public static Error UserNotFound() => Error.NotFound("User not found.");
    public static Error NoScope() => new("AC_NO_WORKSPACE", "No workspace scope on the request.", ErrorType.Forbidden);
    public static Error CodeTaken() => new("AC_CODE_TAKEN", "A role with this code already exists.", ErrorType.Conflict);
    public static Error SystemRoleImmutable() => new("AC_SYSTEM_ROLE", "System roles cannot be modified.", ErrorType.Conflict);
    public static Error UnknownPermission() => new("AC_UNKNOWN_PERMISSION", "One or more permissions do not exist.", ErrorType.Validation);
    public static Error RoleInUse() => new("AC_ROLE_IN_USE", "Cannot delete a role that is assigned to users.", ErrorType.Conflict);
}
