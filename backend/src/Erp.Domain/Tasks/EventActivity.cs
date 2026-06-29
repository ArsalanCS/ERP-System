using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>Kinds of user-visible activity shown on the Logs tab.</summary>
public enum EventActivityKind
{
    Created = 0,
    Updated = 1,
    Assigned = 2,
    StatusChanged = 3,
    PriorityChanged = 4,
    Scheduled = 5,
    Archived = 6,
    SubtaskAdded = 7,
    NoteAdded = 8,
    DocumentAdded = 9,
    RelationChanged = 10,
    DailyReportAdded = 11,
}

/// <summary>
/// User-visible activity trail for an event (Logs tab). Distinct from the protected
/// append-only audit log. Status history is also captured here in addition to
/// <c>EventStatus</c> so the Logs tab can render a single feed.
/// </summary>
public sealed class EventActivity : TenantEntity
{
    private EventActivity() { } // EF

    public EventActivity(long workspaceId, long eventId, EventActivityKind kind, string message, long? actorId, DateTimeOffset occurredAt)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        Kind = kind;
        Message = message;
        ActorId = actorId;
        OccurredAt = occurredAt;
    }

    public long EventId { get; private set; }
    public EventActivityKind Kind { get; private set; }
    public string Message { get; private set; } = default!;
    public long? ActorId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public long? FromStatusId { get; private set; }
    public long? ToStatusId { get; private set; }

    public EventActivity WithStatusChange(long? fromStatusId, long? toStatusId)
    {
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
        return this;
    }
}
