using Erp.Application.Common;
using Erp.Domain.Mail;

namespace Erp.Application.Mail.Contracts;

/// <summary>Filter for the outbox list.</summary>
public sealed record OutboxQuery : ListQuery
{
    public SendStatus? Status { get; init; }
}

public sealed record SendMailListItemDto(
    long Id,
    string Subject,
    SendStatus Status,
    string? TemplateCode,
    int RecipientCount,
    int RetryCount,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    DateTimeOffset CreatedAt);

public sealed record SendMailRecipientDto(string Address, string? DisplayName, MailRecipientKind Kind);

public sealed record SendMailAttemptDto(long Id, int AttemptNo, bool Success, string? ProviderResponse, string? ErrorMessage, DateTimeOffset AttemptedAt);

public sealed record SendMailDetailDto(
    long Id,
    string Subject,
    string BodyHtml,
    string? BodyText,
    string? TemplateDataJson,
    SendStatus Status,
    string? TemplateCode,
    int RetryCount,
    int MaxRetries,
    DateTimeOffset ScheduledAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SendMailRecipientDto> Recipients,
    IReadOnlyList<SendMailAttemptDto> Attempts);

public sealed record MailTemplateDto(
    long Id,
    string Code,
    string Name,
    string SubjectTemplate,
    string BodyHtmlTemplate,
    string? BodyTextTemplate,
    bool IsActive,
    bool IsGlobal,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateMailTemplateRequest(string Name, string SubjectTemplate, string BodyHtmlTemplate, string? BodyTextTemplate, bool IsActive);
