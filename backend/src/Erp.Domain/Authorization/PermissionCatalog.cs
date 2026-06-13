namespace Erp.Domain.Authorization;

/// <summary>
/// The global catalog of permission codes for the Identity module (spec §5.2).
/// Seeded into the <c>permissions</c> table at startup and referenced by
/// <c>[RequirePermission]</c>. Codes are stable: module.resource.action style,
/// shortened to module.action where the resource equals the module.
/// </summary>
public static class PermissionCatalog
{
    public sealed record Definition(string Code, string Module, string Resource, string Action, bool IsHighRisk = false);

    public const string AdminOverviewView = "admin.overview.view";

    public const string UserView = "user.view";
    public const string UserManage = "user.manage";
    public const string UserInvite = "user.invite";
    public const string UserExport = "user.export";

    public const string RoleView = "role.view";
    public const string RoleManage = "role.manage";

    public const string StructureView = "structure.view";
    public const string StructureManage = "structure.manage";

    public const string SecurityView = "security.view";
    public const string SecurityManage = "security.manage";

    public const string AuditView = "audit.view";
    public const string AuditExport = "audit.export";

    public const string SettingsView = "settings.view";
    public const string SettingsManage = "settings.manage";

    public static readonly IReadOnlyList<Definition> All =
    [
        new(AdminOverviewView, "Admin", "Dashboard", "View"),

        new(UserView, "Users", "User", "View"),
        new(UserManage, "Users", "User", "Manage", IsHighRisk: true),
        new(UserInvite, "Users", "Invitation", "Create"),
        new(UserExport, "Users", "User", "Export"),

        new(RoleView, "AccessControl", "Role", "View"),
        new(RoleManage, "AccessControl", "Role", "Manage", IsHighRisk: true),

        new(StructureView, "BusinessStructure", "Structure", "View"),
        new(StructureManage, "BusinessStructure", "Structure", "Manage", IsHighRisk: true),

        new(SecurityView, "Security", "Security", "View"),
        new(SecurityManage, "Security", "Security", "Manage", IsHighRisk: true),

        new(AuditView, "Audit", "AuditLog", "View"),
        new(AuditExport, "Audit", "AuditLog", "Export", IsHighRisk: true),

        new(SettingsView, "Settings", "Setting", "View"),
        new(SettingsManage, "Settings", "Setting", "Manage", IsHighRisk: true),
    ];
}
