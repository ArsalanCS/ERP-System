using Erp.Domain.Common;

namespace Erp.Domain.Authorization;

/// <summary>
/// Assigns a role to a user, optionally scoped to a single cluster. One user may
/// hold different roles per cluster in the same workspace (Identity spec §4.2).
/// A null <see cref="ClusterId"/> means the role applies workspace-wide.
/// </summary>
public sealed class UserRole : TenantEntity
{
    private UserRole() { } // EF

    public UserRole(long workspaceId, long userId, long roleId, long? clusterId = null)
    {
        AssignWorkspace(workspaceId);
        UserId = userId;
        RoleId = roleId;
        ClusterId = clusterId;
    }

    public long UserId { get; private set; }
    public long RoleId { get; private set; }
    public long? ClusterId { get; private set; }
}
