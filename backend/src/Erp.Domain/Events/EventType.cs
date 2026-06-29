using Erp.Domain.Common;

namespace Erp.Domain.Events;

/// <summary>
/// Global lookup describing the kind of <see cref="Event"/> (Event/Asset architecture §6).
/// Code-driven (resolve by <see cref="Code"/>, never hard-coded ids); seeded once at startup.
/// Not tenant-owned — the catalogue is identical for every workspace.
/// </summary>
public sealed class EventType : Entity
{
    private EventType() { } // EF

    public EventType(string code, string name)
    {
        Code = code.Trim();
        Name = name.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
}

/// <summary>Stable event-type codes. Only TASK_MANAGEMENT is active this phase.</summary>
public static class EventTypeCodes
{
    public const string TaskManagement = "TASK_MANAGEMENT";
    public const string Issue = "ISSUE";
    public const string Maintenance = "MAINTENANCE";
    public const string Approval = "APPROVAL";
    public const string SalesActivity = "SALES_ACTIVITY";
}
