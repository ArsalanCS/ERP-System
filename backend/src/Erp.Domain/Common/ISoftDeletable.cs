namespace Erp.Domain.Common;

/// <summary>
/// Identity and structural records referenced by historical transactions are
/// archived/soft-deleted, never hard-deleted (CLAUDE.md §4.9). A global query
/// filter hides soft-deleted rows by default.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
    Guid? DeletedBy { get; }
}
