using Erp.Domain.Common;

namespace Erp.Domain.Workflow;

/// <summary>
/// Workflow status history for an event (Event/Asset architecture §15). Exactly one row
/// per event has <see cref="IsCurrent"/> = true. Changing status supersedes the previous
/// current row and inserts a new current row.
/// </summary>
public sealed class EventStatus : TenantEntity
{
    private EventStatus() { } // EF

    public EventStatus(long workspaceId, long eventId, long statusId, string? note)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        StatusId = statusId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        IsCurrent = true;
    }

    public long EventId { get; private set; }
    public long StatusId { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? Note { get; private set; }

    public void Supersede() => IsCurrent = false;
}
