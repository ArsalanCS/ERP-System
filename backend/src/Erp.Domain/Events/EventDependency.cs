using Erp.Domain.Common;

namespace Erp.Domain.Events;

/// <summary>
/// A dependency between two events/tasks ("this depends on that") used by the Gantt and
/// Relations tabs. The Event/Asset architecture covers event↔asset links via EventAsset;
/// event↔event dependencies are modelled here.
/// </summary>
public sealed class EventDependency : TenantEntity
{
    private EventDependency() { } // EF

    public EventDependency(long workspaceId, long eventId, long dependsOnEventId, bool isBlocking)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        DependsOnEventId = dependsOnEventId;
        IsBlocking = isBlocking;
    }

    public long EventId { get; private set; }
    public long DependsOnEventId { get; private set; }
    public bool IsBlocking { get; private set; }
}
