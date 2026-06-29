using Erp.Domain.Common;

namespace Erp.Domain.Identity;

/// <summary>
/// Employee / HR details for a user account, kept in its own table so the auth
/// <see cref="User"/> record stays focused on identity. One row per user
/// (1:1 via <see cref="UserId"/>). The employee's spot in the business-structure
/// tree is its <see cref="PlacementNodeId"/> (Identity spec §6 "members").
/// </summary>
public sealed class Employee : TenantEntity
{
    private Employee() { } // EF

    public Employee(long workspaceId, long userId)
    {
        AssignWorkspace(workspaceId);
        UserId = userId;
    }

    /// <summary>The auth account this employee record belongs to (unique).</summary>
    public long UserId { get; private set; }

    public string? EmployeeNumber { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Mobile { get; private set; }

    /// <summary>The structure node the employee is placed under (org/dept/branch/team…).</summary>
    public long? PlacementNodeId { get; private set; }

    /// <summary>The user id of this employee's manager.</summary>
    public long? ManagerId { get; private set; }

    public DateTimeOffset? HireDate { get; private set; }

    public void UpdateDetails(string? employeeNumber, string? jobTitle, string? mobile, DateTimeOffset? hireDate)
    {
        EmployeeNumber = employeeNumber;
        JobTitle = jobTitle;
        Mobile = mobile;
        HireDate = hireDate;
    }

    /// <summary>Sets just the contact/title fields edited from the self-service profile page.</summary>
    public void SetContact(string? jobTitle, string? mobile)
    {
        JobTitle = jobTitle;
        Mobile = mobile;
    }

    public void PlaceAt(long? nodeId) => PlacementNodeId = nodeId;

    public void SetManager(long? managerId) => ManagerId = managerId;
}
