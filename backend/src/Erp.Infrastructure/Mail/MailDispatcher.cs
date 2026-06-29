using Erp.Application.Abstractions;
using Erp.Domain.Mail;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Mail;

/// <summary>
/// Drains due outbox messages (Mail doc §13): claims each as PROCESSING, delivers every recipient via
/// <see cref="IEmailSender"/>, records a <see cref="SendMailAttempt"/>, and marks the message SENT or
/// schedules a retry with backoff. The caller establishes a platform-admin tenant scope so it can
/// dispatch across all workspaces; RLS is bypassed only for this trusted server-side job.
/// </summary>
public sealed class MailDispatcher(
    ErpDbContext db,
    IEmailSender sender,
    ILogger<MailDispatcher> logger) : IMailDispatcher
{
    public async Task<int> DispatchDueAsync(int batchSize = 50, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var due = await db.SendMails
            .Where(m => m.Status == SendStatus.Pending && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.ScheduledAt)
            .Take(batchSize)
            .ToListAsync(ct);
        if (due.Count == 0) return 0;

        foreach (var mail in due)
        {
            var recipients = await db.SendMailRecipients.Where(r => r.SendMailId == mail.Id).ToListAsync(ct);

            mail.MarkProcessing();
            var attemptNo = mail.RetryCount + 1;
            try
            {
                foreach (var r in recipients)
                    await sender.SendMessageAsync(r.Address, mail.Subject, mail.BodyHtml, ct);

                mail.MarkSent(DateTimeOffset.UtcNow);
                db.SendMailAttempts.Add(new SendMailAttempt(mail.WorkspaceId, mail.Id, attemptNo, true,
                    providerResponse: $"Delivered to {recipients.Count} recipient(s).", errorMessage: null, DateTimeOffset.UtcNow));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                mail.MarkFailed(ex.Message, DateTimeOffset.UtcNow);
                db.SendMailAttempts.Add(new SendMailAttempt(mail.WorkspaceId, mail.Id, attemptNo, false,
                    providerResponse: null, errorMessage: ex.Message, DateTimeOffset.UtcNow));
                logger.LogWarning(ex, "Failed to deliver mail {MailId} (attempt {Attempt}/{Max}).",
                    mail.Id, mail.RetryCount, mail.MaxRetries);
            }
        }

        await db.SaveChangesAsync(ct);
        return due.Count;
    }
}
