using Erp.Domain.Common;

namespace Erp.Domain.Authorization;

/// <summary>
/// A named permission bundle within a workspace (Identity spec §5.4). System
/// roles are seeded templates; custom roles are tenant-defined.
/// </summary>
public sealed class Role : TenantEntity
{
    private readonly List<RolePermission> _permissions = [];

    private Role() { } // EF

    public Role(Guid workspaceId, string name, string code, RoleType type, string? description = null)
    {
        AssignWorkspace(workspaceId);
        Name = name;
        Code = code;
        Type = type;
        Description = description;
    }

    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public RoleType Type { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public bool IsSystem => Type == RoleType.System;

    public void Update(string name, string? description, string? color)
    {
        EnsureMutable();
        Name = name;
        Description = description;
        Color = color;
    }

    public void Grant(Guid permissionId, DataScope scope)
    {
        EnsureMutable();
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing is null)
        {
            _permissions.Add(new RolePermission(WorkspaceId, Id, permissionId, scope));
        }
        else
        {
            existing.ChangeScope(scope);
        }
    }

    public void Revoke(Guid permissionId)
    {
        EnsureMutable();
        _permissions.RemoveAll(p => p.PermissionId == permissionId);
    }

    public void ClearPermissions()
    {
        EnsureMutable();
        _permissions.Clear();
    }

    private void EnsureMutable()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System roles cannot be modified.");
        }
    }
}
