using Erp.Domain.Common;

namespace Erp.Domain.Workflow;

/// <summary>
/// A category of statuses (Event/Asset architecture §14): TASK_STATUS (workflow),
/// TASK_PRIORITY, ASSET_STATUS, etc. Tenant-owned so each workspace can manage its own
/// statuses; seeded with defaults on workspace provisioning.
/// </summary>
public sealed class StatusType : TenantEntity
{
    private StatusType() { } // EF

    public StatusType(long workspaceId, string code, string name)
    {
        AssignWorkspace(workspaceId);
        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public void Rename(string name) => Name = name.Trim();
}

/// <summary>Stable status-type codes.</summary>
public static class StatusTypeCodes
{
    public const string TaskStatus = "TASK_STATUS";
    public const string TaskPriority = "TASK_PRIORITY";
    public const string AssetStatus = "ASSET_STATUS";
}
