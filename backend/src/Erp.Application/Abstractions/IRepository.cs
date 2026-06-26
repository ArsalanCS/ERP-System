using Erp.Domain.Common;

namespace Erp.Application.Abstractions;

/// <summary>
/// Generic persistence boundary for an aggregate (Refactor Guide §7.1). Services
/// compose reads through <see cref="Query"/> (tenant + soft-delete filters are
/// applied centrally by the DbContext) and mutate via <see cref="Add"/>/<see cref="Remove"/>.
/// Register once as an open generic: <c>IRepository&lt;&gt; -&gt; EfRepository&lt;&gt;</c>.
/// Hand-written, feature-specific repositories remain only for special queries.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>Queryable root; global query filters (tenant + soft-delete) already applied.</summary>
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    void Add(TEntity entity);

    void Remove(TEntity entity);
}
