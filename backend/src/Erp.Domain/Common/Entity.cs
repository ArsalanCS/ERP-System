namespace Erp.Domain.Common;

/// <summary>
/// Base for all persisted entities: a time-ordered <see cref="Id"/> (UUID v7,
/// index-friendly in Postgres), audit metadata, and an optimistic-concurrency
/// <see cref="Version"/> (mapped to Postgres <c>xmin</c> in infrastructure).
/// </summary>
public abstract class Entity
{
    protected Entity() => Id = Guid.CreateVersion7();

    protected Entity(Guid id) => Id = id;

    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    /// <summary>Optimistic-concurrency token (Postgres xmin). Read-only to callers.</summary>
    public uint Version { get; private set; }
}

/// <summary>
/// Base for tenant-owned, soft-deletable entities. Carries <see cref="WorkspaceId"/>
/// and the soft-delete flags enforced by global query filters + RLS.
/// </summary>
public abstract class TenantEntity : Entity, ITenantOwned, ISoftDeletable
{
    protected TenantEntity() { }

    protected TenantEntity(Guid id) : base(id) { }

    public Guid WorkspaceId { get; protected set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>Soft-delete (archive). Use instead of hard delete (CLAUDE.md §4.9).</summary>
    public void SoftDelete(Guid? deletedBy, DateTimeOffset when)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = when;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }

    /// <summary>Assigns the owning workspace. Set once at creation by the domain.</summary>
    protected void AssignWorkspace(Guid workspaceId) => WorkspaceId = workspaceId;
}
