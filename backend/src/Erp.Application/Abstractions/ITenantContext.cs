namespace Erp.Application.Abstractions;

/// <summary>
/// The active tenant scope for the current unit of work. Drives BOTH layers of
/// isolation (CLAUDE.md §4.1): the EF Core global query filter and the
/// PostgreSQL RLS session variables.
///
/// Normally populated by middleware from the authenticated JWT. The login flow
/// sets it explicitly (to the workspace resolved from the slug) so the pre-auth
/// user lookup is correctly scoped.
/// </summary>
public interface ITenantContext
{
    Guid? WorkspaceId { get; }
    IReadOnlySet<Guid> ClusterIds { get; }

    /// <summary>Platform super admin bypasses tenant scoping (cross-tenant ops).</summary>
    bool IsPlatformAdmin { get; }

    bool HasScope { get; }

    /// <summary>Establishes the tenant scope for this request/unit of work.</summary>
    void SetScope(Guid workspaceId, IEnumerable<Guid> clusterIds, bool isPlatformAdmin = false);

    /// <summary>
    /// Temporarily overrides the scope (e.g. the login flow targeting a workspace
    /// before the caller is authenticated). Restores the previous scope on dispose.
    /// </summary>
    IDisposable BeginScope(Guid workspaceId, IEnumerable<Guid>? clusterIds = null, bool isPlatformAdmin = false);
}
