namespace Erp.Domain.Identity;

/// <summary>Account status (Identity spec §4.3). Only <see cref="Active"/> may log in.</summary>
public enum UserStatus
{
    PendingInvitation = 0,
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Locked = 4,
    Archived = 5,
}
