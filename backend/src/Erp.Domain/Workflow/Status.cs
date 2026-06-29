using Erp.Domain.Common;

namespace Erp.Domain.Workflow;

/// <summary>
/// A single status value under a <see cref="StatusType"/> (Event/Asset architecture §14).
/// For TASK_STATUS: <see cref="IsInitial"/> marks the starting status and
/// <see cref="IsClosed"/> marks terminal/closed states (Done/Cancelled/Rejected) used for
/// completed/overdue calculations and closed-status edit protection.
/// </summary>
public sealed class Status : TenantEntity
{
    private Status() { } // EF

    public Status(Guid workspaceId, Guid statusTypeId, string code, string name, int sortOrder, bool isInitial, bool isClosed, string? color)
    {
        AssignWorkspace(workspaceId);
        StatusTypeId = statusTypeId;
        Code = code.Trim();
        Name = name.Trim();
        SortOrder = sortOrder;
        IsInitial = isInitial;
        IsClosed = isClosed;
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        IsActive = true;
    }

    public Guid StatusTypeId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }
    public bool IsInitial { get; private set; }
    public bool IsClosed { get; private set; }
    public string? Color { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, int sortOrder, bool isInitial, bool isClosed, string? color, bool isActive)
    {
        Name = name.Trim();
        SortOrder = sortOrder;
        IsInitial = isInitial;
        IsClosed = isClosed;
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        IsActive = isActive;
    }

    public void ClearInitial() => IsInitial = false;

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
