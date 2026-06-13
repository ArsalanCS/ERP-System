using Erp.Domain.Auditing;

namespace Erp.Application.Abstractions;

public sealed record AuditQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? ActorUserId,
    string? Action,
    string? Module,
    AuditResult? Result,
    int Page,
    int PageSize);

public interface IAuditRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
