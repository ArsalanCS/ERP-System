using Erp.Application.Abstractions;
using Erp.Domain.Authorization;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(ErpDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}

public sealed class WorkspaceRepository(ErpDbContext context) : IWorkspaceRepository
{
    public Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => context.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug, cancellationToken);

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Workspaces.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public void Add(Workspace workspace) => context.Workspaces.Add(workspace);
}

public sealed class UserRepository(ErpDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(Guid workspaceId, string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        // Explicit workspace filter in addition to the global filter + RLS (CONVENTIONS).
        return context.Users.FirstOrDefaultAsync(
            u => u.WorkspaceId == workspaceId && u.NormalizedEmail == normalized, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(Guid workspaceId, string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return context.Users.AnyAsync(
            u => u.WorkspaceId == workspaceId && u.NormalizedEmail == normalized, cancellationToken);
    }

    public void Add(User user) => context.Users.Add(user);

    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
        string? search, UserStatus? status, string? sort, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.DisplayName, term) ||
                EF.Functions.ILike(u.Email, term) ||
                (u.JobTitle != null && EF.Functions.ILike(u.JobTitle, term)));
        }

        if (status is { } s)
        {
            query = query.Where(u => u.Status == s);
        }

        var total = await query.CountAsync(cancellationToken);

        query = sort switch
        {
            "email" => query.OrderBy(u => u.Email),
            "-email" => query.OrderByDescending(u => u.Email),
            "name" => query.OrderBy(u => u.DisplayName),
            "-lastLogin" => query.OrderByDescending(u => u.LastLoginAt),
            _ => query.OrderByDescending(u => u.CreatedAt),
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).Distinct().ToListAsync(cancellationToken);

    public Task<int> CountActiveByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Join(context.Users.Where(u => u.Status == UserStatus.Active), ur => ur.UserId, u => u.Id, (ur, u) => u.Id)
            .Distinct()
            .CountAsync(cancellationToken);
}

public sealed class RoleRepository(ErpDbContext context) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PermissionGrantData>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
        => await context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new PermissionGrantData(rp.PermissionId, rp.Scope))
            .ToListAsync(cancellationToken);

    public async Task ReplaceRolePermissionsAsync(Guid workspaceId, Guid roleId, IReadOnlyCollection<PermissionGrantData> grants, CancellationToken cancellationToken = default)
    {
        var existing = await context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        context.RolePermissions.RemoveRange(existing);
        foreach (var grant in grants)
        {
            context.RolePermissions.Add(new RolePermission(workspaceId, roleId, grant.PermissionId, grant.Scope));
        }
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
        => await context.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        => await context.Roles.Where(r => ids.Contains(r.Id)).Select(r => r.Id).ToListAsync(cancellationToken);

    public void Add(Role role) => context.Roles.Add(role);

    public void Remove(Role role) => context.Roles.Remove(role);

    public async Task SetUserRolesAsync(Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var existing = await context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);
        context.UserRoles.RemoveRange(existing);
        foreach (var roleId in roleIds.Distinct())
        {
            context.UserRoles.Add(new UserRole(workspaceId, userId, roleId));
        }
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        => context.Roles.AnyAsync(r => r.Code == code, cancellationToken);

    public Task<int> CountAssignmentsAsync(Guid roleId, CancellationToken cancellationToken = default)
        => context.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).Distinct().CountAsync(cancellationToken);
}

public sealed class PermissionRepository(ErpDbContext context) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken = default)
        => await context.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> FilterExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        => await context.Permissions.Where(p => ids.Contains(p.Id)).Select(p => p.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserPermission>> ListUserOverridesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserPermissions.Where(up => up.UserId == userId).ToListAsync(cancellationToken);

    public void AddUserOverride(UserPermission overrideEntry) => context.UserPermissions.Add(overrideEntry);

    public async Task RemoveUserOverridesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await context.UserPermissions.Where(up => up.UserId == userId).ToListAsync(cancellationToken);
        context.UserPermissions.RemoveRange(existing);
    }
}

public sealed class RefreshTokenRepository(ErpDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(now);
        }
    }

    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> RevokeByIdAsync(Guid id, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
        if (token is null) return false;
        token.Revoke(now);
        return true;
    }
}

public sealed class PasswordResetTokenRepository(ErpDbContext context) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(PasswordResetToken token) => context.PasswordResetTokens.Add(token);
}

public sealed class SecurityPolicyRepository(ErpDbContext context) : ISecurityPolicyRepository
{
    public Task<WorkspaceSecurityPolicy?> GetForWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => context.WorkspaceSecurityPolicies.FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId, cancellationToken);

    public void Add(WorkspaceSecurityPolicy policy) => context.WorkspaceSecurityPolicies.Add(policy);
}
