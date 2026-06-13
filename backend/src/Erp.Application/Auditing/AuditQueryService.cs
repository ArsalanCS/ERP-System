using System.Text.RegularExpressions;
using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Auditing;

namespace Erp.Application.Auditing;

public sealed record AuditLogDto(
    Guid Id, DateTimeOffset OccurredAt, Guid? ActorUserId, string? ActorDisplayName,
    string Action, string Module, string ResourceType, string? ResourceId,
    AuditResult Result, AuditSource Source, string? IpAddress, string CorrelationId,
    string? Reason, string? OldValues, string? NewValues);

public sealed record AuditSearchQuery : ListQuery
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? Action { get; init; }
    public string? Module { get; init; }
    public AuditResult? Result { get; init; }
}

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogDto>> SearchAsync(AuditSearchQuery query, bool canSeeSensitive, CancellationToken ct = default);

    /// <summary>Exports audit logs and records the export as its own audit event (spec §8.3).</summary>
    Task<IReadOnlyList<AuditLogDto>> ExportAsync(AuditSearchQuery query, bool canSeeSensitive, CancellationToken ct = default);
}

/// <summary>
/// Audit & Activity read API (Identity spec §8). Sensitive fields are masked
/// unless the caller is permitted (CLAUDE.md §4.3); exporting writes an EXPORT
/// audit event.
/// </summary>
public sealed partial class AuditQueryService(
    IAuditRepository repo,
    IAuditLogger audit,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IAuditQueryService
{
    // Keys whose values are masked in audit display unless the caller is permitted.
    private static readonly string[] SensitiveKeys = ["salary", "bank_iban", "iban", "national_id", "iqama_no", "password", "token", "secret"];

    public async Task<PagedResult<AuditLogDto>> SearchAsync(AuditSearchQuery query, bool canSeeSensitive, CancellationToken ct = default)
    {
        var (items, total) = await repo.QueryAsync(ToQuery(query), ct);
        var mapped = items.Select(a => Map(a, canSeeSensitive)).ToList();
        return new PagedResult<AuditLogDto>(mapped, query.Page, query.PageSize, total);
    }

    public async Task<IReadOnlyList<AuditLogDto>> ExportAsync(AuditSearchQuery query, bool canSeeSensitive, CancellationToken ct = default)
    {
        // Export the matching window (cap large exports via a generous page size).
        var exportQuery = query with { Page = 1, PageSize = 5000 };
        var (items, _) = await repo.QueryAsync(ToQuery(exportQuery), ct);

        await audit.LogAsync(new AuditEntry
        {
            Action = AuditActions.Export, Module = "Audit", ResourceType = "AuditLog",
            WorkspaceId = tenant.WorkspaceId, Reason = $"Exported {items.Count} audit records",
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return items.Select(a => Map(a, canSeeSensitive)).ToList();
    }

    private static AuditQuery ToQuery(AuditSearchQuery q) =>
        new(q.From, q.To, q.ActorUserId, q.Action, q.Module, q.Result, q.Page, q.PageSize);

    private static AuditLogDto Map(AuditLog a, bool canSeeSensitive) => new(
        a.Id, a.OccurredAt, a.ActorUserId, a.ActorDisplayName, a.Action, a.Module, a.ResourceType, a.ResourceId,
        a.Result, a.Source, a.IpAddress, a.CorrelationId, a.Reason,
        canSeeSensitive ? a.OldValues : Mask(a.OldValues),
        canSeeSensitive ? a.NewValues : Mask(a.NewValues));

    private static string? Mask(string? json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        return SensitiveValue().Replace(json, m =>
        {
            var key = m.Groups["key"].Value;
            return SensitiveKeys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase))
                ? $"\"{key}\":\"***\""
                : m.Value;
        });
    }

    [GeneratedRegex("\"(?<key>[^\"]+)\"\\s*:\\s*\"(?<val>(?:[^\"\\\\]|\\\\.)*)\"")]
    private static partial Regex SensitiveValue();
}
