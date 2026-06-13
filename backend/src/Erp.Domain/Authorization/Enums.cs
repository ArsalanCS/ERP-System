namespace Erp.Domain.Authorization;

/// <summary>
/// Data-scope dimension of a permission (Identity spec §5.2). Widening order:
/// Own &lt; Team &lt; Department &lt; Cluster &lt; Organization &lt; Workspace &lt; AllTenants.
/// </summary>
public enum DataScope
{
    Own = 0,
    Team = 1,
    Department = 2,
    Cluster = 3,
    Organization = 4,
    Workspace = 5,
    AllTenants = 6,
}

/// <summary>Allow or explicit Deny. Deny overrides Allow (spec §5.2).</summary>
public enum PermissionEffect
{
    Allow = 1,
    Deny = 2,
}

/// <summary>Origin/scope of a role (spec §5.4).</summary>
public enum RoleType
{
    /// <summary>Platform-defined template (not editable by tenants).</summary>
    System = 0,
    Workspace = 1,
    Organization = 2,
    Custom = 3,
}
