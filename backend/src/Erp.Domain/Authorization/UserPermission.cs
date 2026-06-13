using Erp.Domain.Common;

namespace Erp.Domain.Authorization;

/// <summary>
/// An explicit per-user permission override (Identity spec §5.1 User Overrides).
/// A <see cref="PermissionEffect.Deny"/> override wins over any role-granted
/// allow (spec §5.2).
/// </summary>
public sealed class UserPermission : TenantEntity
{
    private UserPermission() { } // EF

    public UserPermission(Guid workspaceId, Guid userId, Guid permissionId, PermissionEffect effect, DataScope scope = DataScope.Own)
    {
        AssignWorkspace(workspaceId);
        UserId = userId;
        PermissionId = permissionId;
        Effect = effect;
        Scope = scope;
    }

    public Guid UserId { get; private set; }
    public Guid PermissionId { get; private set; }
    public PermissionEffect Effect { get; private set; }
    public DataScope Scope { get; private set; }

    public void Change(PermissionEffect effect, DataScope scope)
    {
        Effect = effect;
        Scope = scope;
    }
}
