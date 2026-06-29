namespace Erp.Domain.Common;

/// <summary>
/// Company-standard base for all persisted entities: a BigInt <see cref="Id"/>
/// (Postgres <c>identity</c>, mapped to C# <see cref="long"/>), the active/deleted
/// flags, and the inserted/changed audit metadata. An optimistic-concurrency
/// <see cref="Version"/> (Postgres <c>xmin</c>) is kept internally to satisfy
/// CLAUDE.md §5; it is never exposed in DTOs.
/// </summary>
public abstract class BaseEntity
{
    protected BaseEntity() => Id = IdGenerator.NewId();

    protected BaseEntity(long id) => Id = id;

    public long Id { get; protected set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; private set; }

    public long? InsertedBy { get; set; }
    public DateTimeOffset InsertedDate { get; set; }
    public long? ChangedBy { get; set; }
    public DateTimeOffset? ChangedDate { get; set; }

    /// <summary>Optimistic-concurrency token (Postgres xmin). Read-only to callers.</summary>
    public uint Version { get; private set; }

    /// <summary>Soft-delete (archive). Use instead of hard delete (CLAUDE.md §4.9).</summary>
    public void SoftDelete(long? deletedBy, DateTimeOffset when)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
        ChangedBy = deletedBy;
        ChangedDate = when;
    }

    public void Restore()
    {
        IsDeleted = false;
        IsActive = true;
    }
}

/// <summary>
/// Base for tenant-owned entities. Adds <see cref="WorkspaceId"/>; isolation is
/// enforced in two layers — PostgreSQL RLS and backend query filters (CLAUDE.md §4.1).
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantOwned
{
    protected TenantEntity() { }

    protected TenantEntity(long id) : base(id) { }

    public long WorkspaceId { get; protected set; }

    /// <summary>Assigns the owning workspace. Set once at creation by the domain.</summary>
    protected void AssignWorkspace(long workspaceId) => WorkspaceId = workspaceId;
}
