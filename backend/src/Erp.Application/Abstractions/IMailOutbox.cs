using Erp.Domain.Mail;

namespace Erp.Application.Abstractions;

/// <summary>
/// Enqueues notification emails onto the outbox (send_mails). The message is persisted as part of
/// the caller's unit of work and delivered later by the dispatcher worker — never sent inside the
/// business transaction (Mail doc §16). Queuing is best-effort; a failure here never aborts the
/// surrounding operation.
/// </summary>
public interface IMailOutbox
{
    /// <summary>
    /// Resolves the template for <see cref="MailRequest.TemplateCode"/> (workspace override first, else
    /// global default; falling back to the supplied subject/body when none is configured), substitutes
    /// placeholders, and adds a <c>send_mail</c> + its recipients to the context. Stores the placeholder
    /// values as <c>template_data_json</c>. No-op when there are no recipients. Does not SaveChanges.
    /// </summary>
    Task QueueAsync(MailRequest request, CancellationToken ct = default);
}

/// <summary>A recipient for an outbox message.</summary>
public sealed record MailRecipientInput(string Address, string? DisplayName, MailRecipientKind Kind = MailRecipientKind.To);

/// <summary>A request to enqueue a templated notification. Carries no event/asset FK (Mail doc §7).</summary>
public sealed record MailRequest(
    Guid WorkspaceId,
    string TemplateCode,
    string FallbackSubject,
    string FallbackBody,
    IReadOnlyDictionary<string, string> Placeholders,
    IReadOnlyList<MailRecipientInput> Recipients);
