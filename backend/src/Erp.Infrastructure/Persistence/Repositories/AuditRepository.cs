using Erp.Application.Abstractions;
using Erp.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(ErpDbContext context) : IAuditRepository
{
    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var q = context.AuditLogs.AsNoTracking();

        if (query.From is { } from) q = q.Where(a => a.OccurredAt >= from);
        if (query.To is { } to) q = q.Where(a => a.OccurredAt <= to);
        if (query.ActorUserId is { } actor) q = q.Where(a => a.ActorUserId == actor);
        if (!string.IsNullOrWhiteSpace(query.Action)) q = q.Where(a => a.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.Module)) q = q.Where(a => a.Module == query.Module);
        if (query.Result is { } result) q = q.Where(a => a.Result == result);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
