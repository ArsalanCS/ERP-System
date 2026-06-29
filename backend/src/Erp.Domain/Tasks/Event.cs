using Erp.Domain.Common;

namespace Erp.Domain.Tasks;

/// <summary>
/// Base event record (Event/Asset architecture §5): minimal by design — it only
/// identifies that an event exists and its <see cref="EventTypeId"/>. Type-specific
/// detail lives in extension tables (e.g. <see cref="TaskEvent"/>). Tenant-owned so
/// the same two-layer isolation (EF query filter + Postgres RLS) applies.
/// </summary>
public sealed class Event : TenantEntity
{
    private Event() { } // EF

    public Event(long workspaceId, long eventTypeId)
    {
        AssignWorkspace(workspaceId);
        EventTypeId = eventTypeId;
    }

    public long EventTypeId { get; private set; }
}
