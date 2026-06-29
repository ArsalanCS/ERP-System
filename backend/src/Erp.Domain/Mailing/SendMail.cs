using Erp.Domain.Common;

namespace Erp.Domain.Mailing;

/// <summary>
/// An outbox message (send mail) — the final rendered email ready for the worker (Mail doc §6).
/// Global by design: it carries NO event/asset-specific foreign keys (§7). The reason an email
/// exists is captured by <see cref="MailTemplateId"/>/template code + <see cref="TemplateDataJson"/>.
/// Delivered asynchronously by the dispatcher with bounded retry/backoff; each attempt is recorded
/// in <see cref="SendMailAttempt"/>.
/// </summary>
public sealed class SendMail : TenantEntity
{
    private SendMail() { } // EF

    public SendMail(long workspaceId, long? mailTemplateId, string? templateCode, string subject,
        string bodyHtml, string? bodyText, string? templateDataJson, DateTimeOffset scheduledAt, int maxRetries = 5)
    {
        AssignWorkspace(workspaceId);
        MailTemplateId = mailTemplateId;
        TemplateCode = templateCode;
        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        TemplateDataJson = templateDataJson;
        ScheduledAt = scheduledAt;
        NextAttemptAt = scheduledAt;
        MaxRetries = maxRetries;
        Status = SendStatus.Pending;
    }

    public long? MailTemplateId { get; private set; }
    /// <summary>The template code that produced this mail (traceability; the doc allows code or id).</summary>
    public string? TemplateCode { get; private set; }
    public string Subject { get; private set; } = default!;
    public string BodyHtml { get; private set; } = default!;
    public string? BodyText { get; private set; }
    /// <summary>JSON of the placeholder values used to render the email (debug/audit, §6).</summary>
    public string? TemplateDataJson { get; private set; }
    public SendStatus Status { get; private set; }

    public DateTimeOffset ScheduledAt { get; private set; }
    /// <summary>When the message is next eligible for an attempt (null once terminal).</summary>
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public string? LastError { get; private set; }

    public void MarkProcessing() => Status = SendStatus.Processing;

    public void MarkSent(DateTimeOffset when)
    {
        Status = SendStatus.Sent;
        SentAt = when;
        NextAttemptAt = null;
        LastError = null;
    }

    /// <summary>Records a failed attempt; schedules a retry with backoff or gives up.</summary>
    public void MarkFailed(string error, DateTimeOffset when)
    {
        RetryCount++;
        LastError = Truncate(error, 2000);
        if (RetryCount >= MaxRetries)
        {
            Status = SendStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            Status = SendStatus.Pending;
            // Exponential backoff: 1, 2, 4, 8 … minutes.
            var minutes = Math.Pow(2, RetryCount - 1);
            NextAttemptAt = when.AddMinutes(minutes);
        }
    }

    /// <summary>Manual requeue of a failed/cancelled message — resets the attempt window.</summary>
    public void Requeue(DateTimeOffset when)
    {
        Status = SendStatus.Pending;
        RetryCount = 0;
        NextAttemptAt = when;
        LastError = null;
    }

    public void Cancel()
    {
        if (Status is SendStatus.Sent) return;
        Status = SendStatus.Cancelled;
        NextAttemptAt = null;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
