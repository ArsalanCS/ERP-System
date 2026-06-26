using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Common;

/// <summary>
/// Shared paging so list endpoints don't repeat Skip/Take/Count (Refactor Guide §7.2).
/// Clamps page/size defensively and returns a <see cref="PagedResult{T}"/>.
/// </summary>
public static class QueryablePagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, ListQuery.MaxPageSize);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, page, pageSize, total);
    }
}
