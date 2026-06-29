using Erp.Domain.Common;

namespace Erp.Domain.Mail;

/// <summary>
/// A reusable email template (Mail doc §4). <see cref="WorkspaceId"/> is nullable: null = a global
/// default available to every workspace; non-null = a workspace-specific override. Subject/body carry
/// <c>{{Placeholder}}</c> tokens substituted when a <see cref="SendMail"/> is rendered.
///
/// Treated as a global catalogue table (soft-deletable, NOT a strict RLS tenant table) so global
/// defaults can be shared across workspaces — flagged to the client. Workspace scoping is applied
/// explicitly in queries (<c>WorkspaceId == null || WorkspaceId == current</c>).
/// </summary>
public sealed class MailTemplate : BaseEntity, ISoftDeletable
{
    private MailTemplate() { } // EF

    public MailTemplate(long? workspaceId, string code, string name, string subjectTemplate,
        string bodyHtmlTemplate, string? bodyTextTemplate)
    {
        WorkspaceId = workspaceId;
        Code = code.Trim();
        Name = name.Trim();
        SubjectTemplate = subjectTemplate;
        BodyHtmlTemplate = bodyHtmlTemplate;
        BodyTextTemplate = bodyTextTemplate;
        IsActive = true;
    }

    /// <summary>Owning workspace; null = shared global default.</summary>
    public long? WorkspaceId { get; private set; }
    public bool IsGlobal => WorkspaceId is null;
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string SubjectTemplate { get; private set; } = default!;
    public string BodyHtmlTemplate { get; private set; } = default!;
    public string? BodyTextTemplate { get; private set; }

    public void Update(string name, string subjectTemplate, string bodyHtmlTemplate, string? bodyTextTemplate, bool isActive)
    {
        Name = name.Trim();
        SubjectTemplate = subjectTemplate;
        BodyHtmlTemplate = bodyHtmlTemplate;
        BodyTextTemplate = bodyTextTemplate;
        IsActive = isActive;
    }

    /// <summary>Creates a workspace-specific override copy of this (global) template.</summary>
    public MailTemplate CreateOverride(long workspaceId) =>
        new(workspaceId, Code, Name, SubjectTemplate, BodyHtmlTemplate, BodyTextTemplate);
}
