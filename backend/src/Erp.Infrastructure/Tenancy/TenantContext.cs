using Erp.Application.Abstractions;

namespace Erp.Infrastructure.Tenancy;

/// <summary>
/// Scoped, mutable <see cref="ITenantContext"/>. One instance per request/unit
/// of work. Set by tenant-resolution middleware (from the JWT) or by the login
/// flow (from the workspace slug).
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private static readonly IReadOnlySet<Guid> Empty = new HashSet<Guid>();

    public Guid? WorkspaceId { get; private set; }
    public IReadOnlySet<Guid> ClusterIds { get; private set; } = Empty;
    public bool IsPlatformAdmin { get; private set; }

    public bool HasScope => WorkspaceId.HasValue || IsPlatformAdmin;

    public void SetScope(Guid workspaceId, IEnumerable<Guid> clusterIds, bool isPlatformAdmin = false)
    {
        WorkspaceId = workspaceId;
        ClusterIds = clusterIds as IReadOnlySet<Guid> ?? new HashSet<Guid>(clusterIds);
        IsPlatformAdmin = isPlatformAdmin;
    }

    public IDisposable BeginScope(Guid workspaceId, IEnumerable<Guid>? clusterIds = null, bool isPlatformAdmin = false)
    {
        var restore = new ScopeRestore(this, WorkspaceId, ClusterIds, IsPlatformAdmin);
        WorkspaceId = workspaceId;
        ClusterIds = clusterIds as IReadOnlySet<Guid> ?? new HashSet<Guid>(clusterIds ?? []);
        IsPlatformAdmin = isPlatformAdmin;
        return restore;
    }

    private sealed class ScopeRestore(
        TenantContext owner,
        Guid? workspaceId,
        IReadOnlySet<Guid> clusterIds,
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
