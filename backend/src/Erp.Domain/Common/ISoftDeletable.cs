namespace Erp.Domain.Common;

/// <summary>
/// Records referenced by historical transactions are archived/soft-deleted, never
/// hard-deleted (CLAUDE.md §4.9). A global query filter hides soft-deleted rows by
/// default. All entities carry <see cref="IsDeleted"/> via <see cref="BaseEntity"/>;
/// this interface marks the ones whose query filter should apply it.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
}
