using Erp.Application.Abstractions;

namespace Erp.Infrastructure.Tenancy;

/// <summary>
/// Scoped, mutable <see cref="ITenantContext"/>. One instance per request/unit
/// of work. Set by tenant-resolution middleware (from the JWT) or by the login
/// flow (from the workspace slug).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly IReadOnlySet<long> Empty = new HashSet<long>();

    public long? WorkspaceId { get; private set; }
    public IReadOnlySet<long> ClusterIds { get; private set; } = Empty;
    public bool IsPlatformAdmin { get; private set; }

    public bool HasScope => WorkspaceId.HasValue || IsPlatformAdmin;

    public void SetScope(long? workspaceId, IEnumerable<long> clusterIds, bool isPlatformAdmin = false)
    {
        WorkspaceId = workspaceId;
        ClusterIds = clusterIds as IReadOnlySet<long> ?? new HashSet<long>(clusterIds);
        IsPlatformAdmin = isPlatformAdmin;
    }

    public IDisposable BeginScope(long? workspaceId, IEnumerable<long>? clusterIds = null, bool isPlatformAdmin = false)
    {
        var restore = new ScopeRestore(this, WorkspaceId, ClusterIds, IsPlatformAdmin);
        WorkspaceId = workspaceId;
        ClusterIds = clusterIds as IReadOnlySet<long> ?? new HashSet<long>(clusterIds ?? []);
        IsPlatformAdmin = isPlatformAdmin;
        return restore;
    }

    private sealed class ScopeRestore(
        TenantContext owner,
        long? workspaceId,
        IReadOnlySet<long> clusterIds,
        bool isPlatformAdmin) : IDisposable
    {
        public void Dispose()
        {
            owner.WorkspaceId = workspaceId;
            owner.ClusterIds = clusterIds;
            owner.IsPlatformAdmin = isPlatformAdmin;
        }
    }
}
