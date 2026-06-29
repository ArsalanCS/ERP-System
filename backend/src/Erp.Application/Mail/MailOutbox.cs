using System.Text.Json;
using System.Text.RegularExpressions;
using Erp.Application.Abstractions;
using Erp.Domain.Mail;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Mail;

/// <summary>
/// Default <see cref="IMailOutbox"/>: resolves the workspace override (else global) template, renders
/// subject + html + text by substituting <c>{{Placeholder}}</c> tokens, stores the placeholder data as
/// JSON, and queues a <c>send_mail</c> + recipients onto the current unit of work for the dispatcher.
/// </summary>
public sealed partial class MailOutbox(
    IRepository<MailTemplate> templates,
    IRepository<SendMail> sendMails,
    IRepository<SendMailRecipient> recipients,
    IClock clock) : IMailOutbox
{
    public async Task QueueAsync(MailRequest request, CancellationToken ct = default)
    {
        var to = request.Recipients
            .Where(r => !string.IsNullOrWhiteSpace(r.Address))
            .GroupBy(r => r.Address.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (to.Count == 0) return;

        // Workspace override takes precedence over the global default for the same code.
        var candidates = await templates.Query()
            .Where(t => t.Code == request.TemplateCode && t.IsActive
                && (t.WorkspaceId == null || t.WorkspaceId == request.WorkspaceId))
            .ToListAsync(ct);
        var template = candidates.FirstOrDefault(t => t.WorkspaceId == request.WorkspaceId)
            ?? candidates.FirstOrDefault(t => t.WorkspaceId == null);

        var subject = Render(template?.SubjectTemplate ?? request.FallbackSubject, request.Placeholders);
        var bodyHtml = Render(template?.BodyHtmlTemplate ?? request.FallbackBody, request.Placeholders);
        var bodyText = template?.BodyTextTemplate is { } txt ? Render(txt, request.Placeholders) : null;
        var dataJson = JsonSerializer.Serialize(request.Placeholders);

        var mail = new SendMail(request.WorkspaceId, template?.Id, request.TemplateCode,
            subject, bodyHtml, bodyText, dataJson, clock.UtcNow);
        sendMails.Add(mail);
        foreach (var r in to)
            recipients.Add(new SendMailRecipient(request.WorkspaceId, mail.Id, r.Address, r.DisplayName, r.Kind));
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> values)
        => TokenPattern().Replace(template, m =>
        {
            var key = m.Groups[1].Value.Trim();
            return values.TryGetValue(key, out var v) ? v : string.Empty;
        });

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}")]
    private static partial Regex TokenPattern();
}
