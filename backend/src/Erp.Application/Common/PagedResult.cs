namespace Erp.Application.Common;

/// <summary>Server-side paged list result (CONVENTIONS.md: paginate all lists).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

/// <summary>Common list query parameters. Page size is capped by the service.</summary>
public record ListQuery
{
    public const int MaxPageSize = 100;

    private readonly int _page = 1;
    private readonly int _pageSize = 25;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is <= 0 ? 25 : Math.Min(value, MaxPageSize);
    }

    public string? Search { get; init; }
    public string? Sort { get; init; }
}
