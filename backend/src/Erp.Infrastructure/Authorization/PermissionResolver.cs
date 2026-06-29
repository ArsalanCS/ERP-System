using Erp.Application.Abstractions;
using Erp.Domain.Authorization;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Authorization;

/// <summary>
/// Computes a user's effective permissions (spec §5.5): union of role-granted
/// permissions and user allow-overrides, then explicit deny-overrides removed
/// (deny wins). The widest data scope per action is kept. Tenant-owned tables
/// are auto-scoped to the active workspace by the global query filter.
/// </summary>
public sealed class PermissionResolver(ErpDbContext context) : IPermissionResolver
{
    public async Task<EffectivePermissions> ResolveAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var rolePermissions = await (
            from rp in context.RolePermissions
            where roleIds.Contains(rp.RoleId)
            join p in context.Permissions on rp.PermissionId equals p.Id
            select new { p.Code, rp.Scope }).ToListAsync(cancellationToken);

        var overrides = await (
            from up in context.UserPermissions
            where up.UserId == userId
            join p in context.Permissions on up.PermissionId equals p.Id
            select new { p.Code, up.Effect, up.Scope }).ToListAsync(cancellationToken);

        var effective = new Dictionary<string, DataScope>(StringComparer.Ordinal);

        foreach (var grant in rolePermissions)
        {
            Widen(effective, grant.Code, grant.Scope);
        }

        foreach (var allow in overrides.Where(o => o.Effect == PermissionEffect.Allow))
        {
            Widen(effective, allow.Code, allow.Scope);
        }

        // Explicit deny wins — applied last so it overrides any allow.
        foreach (var deny in overrides.Where(o => o.Effect == PermissionEffect.Deny))
        {
            effective.Remove(deny.Code);
        }

        return new EffectivePermissions(effective);
    }

    private static void Widen(Dictionary<string, DataScope> map, string code, DataScope scope)
    {
        map[code] = map.TryGetValue(code, out var current) && current >= scope ? current : scope;
    }
}
