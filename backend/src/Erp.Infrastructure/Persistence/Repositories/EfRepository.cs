using Erp.Application.Abstractions;
using Erp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Default <see cref="IRepository{TEntity}"/> over <see cref="ErpDbContext"/>
/// (Refactor Guide §7.1). <see cref="Query"/> returns the DbSet, so the
/// context's global query filters (tenant isolation + soft-delete) and RLS apply.
/// </summary>
public sealed class EfRepository<TEntity>(ErpDbContext db) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    public IQueryable<TEntity> Query() => db.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => db.Set<TEntity>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public void Add(TEntity entity) => db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity) => db.Set<TEntity>().Remove(entity);
}
