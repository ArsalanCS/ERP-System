using Erp.Domain.Common;

namespace Erp.Domain.Authorization;

/// <summary>A permission granted to a role at a given data scope (spec §5.2/§5.3).</summary>
public sealed class RolePermission : TenantEntity
{
    private RolePermission() { } // EF

    public RolePermission(Guid workspaceId, Guid roleId, Guid permissionId, DataScope scope)
    {
        AssignWorkspace(workspaceId);
        RoleId = roleId;
        PermissionId = permissionId;
        Scope = scope;
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DataScope Scope { get; private set; }

    public void ChangeScope(DataScope scope) => Scope = scope;
}
