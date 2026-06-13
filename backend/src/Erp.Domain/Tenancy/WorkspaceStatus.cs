namespace Erp.Domain.Tenancy;

/// <summary>Lifecycle status of a workspace (Identity spec §6.4).</summary>
public enum WorkspaceStatus
{
    Trial = 0,
    Active = 1,
    Suspended = 2,
    Expired = 3,
    Archived = 4,
}
