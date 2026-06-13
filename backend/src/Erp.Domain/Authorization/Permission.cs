using Erp.Domain.Common;

namespace Erp.Domain.Authorization;

/// <summary>
/// An atomic permission in the global catalog (Identity spec §5.2 / §12
/// Permissions): module × resource × action. Platform-defined and shared across
/// all tenants, so it is NOT tenant-owned. <see cref="Code"/> (e.g. "user.manage")
/// is the stable key carried in JWT action claims.
/// </summary>
public sealed class Permission : Entity
{
    private Permission() { } // EF

    public Permission(string code, string module, string resource, string action, bool isHighRisk = false)
    {
        Code = code;
        Module = module;
        Resource = resource;
        Action = action;
        IsHighRisk = isHighRisk;
    }

    public string Code { get; private set; } = null!;
    public string Module { get; private set; } = null!;
    public string Resource { get; private set; } = null!;
    public string Action { get; private set; } = null!;

    /// <summary>Requires confirmation when granted (spec §5.4: Manage Roles, Export All, …).</summary>
    public bool IsHighRisk { get; private set; }
}
