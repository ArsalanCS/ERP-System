using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Mail.Contracts;
using Erp.Domain.Mail;
using Erp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Mail;

/// <summary>
/// Outbox + template management. The outbox is scoped to the caller's workspace; templates show the
/// effective set (workspace override else global default). Editing a global default creates a
/// per-workspace override. Actual delivery is performed by the dispatcher worker.
/// </summary>
public sealed class MailService(
    IRepository<SendMail> sendMails,
    IRepository<SendMailRecipient> recipients,
    IRepository<SendMailAttempt> attempts,
    IRepository<MailTemplate> templates,
    ICurrentUser currentUser,
    IClock clock,
    IUnitOfWork unitOfWork) : IMailService
{
    public async Task<Result<PagedResult<SendMailListItemDto>>> ListOutboxAsync(OutboxQuery query, CancellationToken ct = default)
    {
        var q = sendMails.Query();
        if (query.Status is { } st) q = q.Where(m => m.Status == st);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(m => m.Subject.Contains(term));
        }

        var projected = q.OrderByDescending(m => m.CreatedAt).Select(m => new SendMailListItemDto(
            m.Id, m.Subject, m.Status, m.TemplateCode,
            recipients.Query().Count(r => r.SendMailId == m.Id),
            m.RetryCount, m.ScheduledAt, m.SentAt, m.NextAttemptAt, m.LastError, m.CreatedAt));

        var page = await projected.ToPagedResultAsync(query.Page, query.PageSize, ct);
        return Result.Success(page);
    }

    public async Task<Result<SendMailDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var mail = await sendMails.Query().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mail is null) return Result.Failure<SendMailDetailDto>(MailErrors.NotFound("Message"));

        var recips = await recipients.Query().Where(r => r.SendMailId == id)
            .Select(r => new SendMailRecipientDto(r.Address, r.DisplayName, r.Kind)).ToListAsync(ct);
        var atts = await attempts.Query().Where(a => a.SendMailId == id).OrderByDescending(a => a.AttemptedAt)
            .Select(a => new SendMailAttemptDto(a.Id, a.AttemptNo, a.Success, a.ProviderResponse, a.ErrorMessage, a.AttemptedAt)).ToListAsync(ct);

        return Result.Success(new SendMailDetailDto(
            mail.Id, mail.Subject, mail.BodyHtml, mail.BodyText, mail.TemplateDataJson, mail.Status, mail.TemplateCode,
            mail.RetryCount, mail.MaxRetries, mail.ScheduledAt, mail.SentAt, mail.NextAttemptAt, mail.LastError,
            mail.CreatedAt, recips, atts));
    }

    public async Task<Result> RetryAsync(Guid id, CancellationToken ct = default)
    {
        var mail = await sendMails.Query().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mail is null) return Result.Failure(MailErrors.NotFound("Message"));
        if (mail.Status is not (SendStatus.Failed or SendStatus.Cancelled)) return Result.Failure(MailErrors.CannotRetry());
        mail.Requeue(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var mail = await sendMails.Query().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mail is null) return Result.Failure(MailErrors.NotFound("Message"));
        mail.Cancel();
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MailTemplateDto>>> ListTemplatesAsync(CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure<IReadOnlyList<MailTemplateDto>>(MailErrors.NoScope());

        // Effective set: a workspace override hides the global default for the same code.
        var all = await templates.Query().Where(t => t.WorkspaceId == null || t.WorkspaceId == ws).ToListAsync(ct);
        var effective = all
            .GroupBy(t => t.Code)
            .Select(g => g.FirstOrDefault(t => t.WorkspaceId == ws) ?? g.First(t => t.WorkspaceId == null))
            .OrderBy(t => t.Code)
            .Select(t => new MailTemplateDto(t.Id, t.Code, t.Name, t.SubjectTemplate, t.BodyHtmlTemplate,
                t.BodyTextTemplate, t.IsActive, t.IsGlobal, t.UpdatedAt))
            .ToList();
        return Result.Success<IReadOnlyList<MailTemplateDto>>(effective);
    }

    public async Task<Result> UpdateTemplateAsync(Guid id, UpdateMailTemplateRequest request, CancellationToken ct = default)
    {
        if (currentUser.WorkspaceId is not { } ws) return Result.Failure(MailErrors.NoScope());
        var template = await templates.Query().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return Result.Failure(MailErrors.NotFound("Template"));

        if (template.WorkspaceId == ws)
        {
            template.Update(request.Name, request.SubjectTemplate, request.BodyHtmlTemplate, request.BodyTextTemplate, request.IsActive);
        }
        else
        {
            // Editing a global default (or another scope) creates/updates this workspace's override.
            var existing = await templates.Query().FirstOrDefaultAsync(t => t.WorkspaceId == ws && t.Code == template.Code, ct);
            if (existing is null)
            {
                var ovr = template.CreateOverride(ws);
                ovr.Update(request.Name, request.SubjectTemplate, request.BodyHtmlTemplate, request.BodyTextTemplate, request.IsActive);
                templates.Add(ovr);
            }
            else
            {
                existing.Update(request.Name, request.SubjectTemplate, request.BodyHtmlTemplate, request.BodyTextTemplate, request.IsActive);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
