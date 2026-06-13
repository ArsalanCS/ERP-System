using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;

namespace Erp.Application.Abstractions;

/// <summary>Persistence boundary for the Application layer. Implemented in Infrastructure.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IWorkspaceRepository
{
    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Workspace workspace);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Guid workspaceId, string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Guid workspaceId, string email, CancellationToken cancellationToken = default);
    void Add(User user);

    /// <summary>Paged, filtered, sorted user list (scoped to the active workspace).</summary>
    Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
        string? search, UserStatus? status, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Counts active users holding the given role (for last-owner protection).</summary>
    Task<int> CountActiveByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

/// <summary>A permission grant (permission + data scope) for a role.</summary>
public readonly record struct PermissionGrantData(Guid PermissionId, DataScope Scope);

/// <summary>Read/write of role catalog + per-user role assignments.</summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    void Add(Role role);
    void Remove(Role role);

    /// <summary>Replaces a user's (workspace-wide) role assignments.</summary>
    Task SetUserRolesAsync(Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<int> CountAssignmentsAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionGrantData>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces a role's permission grants (added via DbSet so EF tracks them as inserts).</summary>
    Task ReplaceRolePermissionsAsync(Guid workspaceId, Guid roleId, IReadOnlyCollection<PermissionGrantData> grants, CancellationToken cancellationToken = default);
}

/// <summary>Global permission catalog + per-user permission overrides.</summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserPermission>> ListUserOverridesAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddUserOverride(UserPermission overrideEntry);
    Task RemoveUserOverridesAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);

    /// <summary>Active (non-revoked, unexpired) sessions for a user.</summary>
    Task<IReadOnlyList<RefreshToken>> ListActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Revokes a single session by id, scoped to the owning user. Returns true if found.</summary>
    Task<bool> RevokeByIdAsync(Guid id, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    void Add(PasswordResetToken token);
}

/// <summary>Per-workspace security policy (singleton row per workspace).</summary>
public interface ISecurityPolicyRepository
{
    Task<WorkspaceSecurityPolicy?> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    void Add(WorkspaceSecurityPolicy policy);
}
