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

    public EventDependency(Guid workspaceId, Guid eventId, Guid dependsOnEventId, bool isBlocking)
    {
        AssignWorkspace(workspaceId);
        EventId = eventId;
        DependsOnEventId = dependsOnEventId;
        IsBlocking = isBlocking;
    }

    public Guid EventId { get; private set; }
    public Guid DependsOnEventId { get; private set; }
    public bool IsBlocking { get; private set; }
}
