namespace Erp.Domain.Common;

/// <summary>
/// Marks an entity as owned by a workspace (tenant). Every tenant-owned table
/// carries <see cref="WorkspaceId"/>; isolation is enforced in two layers —
/// PostgreSQL RLS and backend query filters (CLAUDE.md §4.1).
/// </summary>
public interface ITenantOwned
{
    Guid WorkspaceId { get; }
}
