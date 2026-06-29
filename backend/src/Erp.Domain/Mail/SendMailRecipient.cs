using Erp.Domain.Common;

namespace Erp.Domain.Mail;

/// <summary>A recipient of a <see cref="SendMail"/> (To/Cc/Bcc).</summary>
public sealed class SendMailRecipient : TenantEntity
{
    private SendMailRecipient() { } // EF

    public SendMailRecipient(long workspaceId, long sendMailId, string address, string? displayName, MailRecipientKind kind)
    {
        AssignWorkspace(workspaceId);
        SendMailId = sendMailId;
        Address = address.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        Kind = kind;
    }

    public long SendMailId { get; private set; }
    public string Address { get; private set; } = default!;
    public string? DisplayName { get; private set; }
    public MailRecipientKind Kind { get; private set; }
}
