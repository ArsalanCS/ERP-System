using Erp.Domain.Common;

namespace Erp.Domain.Mail;

/// <summary>An individual delivery attempt for a <see cref="SendMail"/> (Mail doc §10 — audit/diagnostics).</summary>
public sealed class SendMailAttempt : TenantEntity
{
    private SendMailAttempt() { } // EF

    public SendMailAttempt(long workspaceId, long sendMailId, int attemptNo, bool success,
        string? providerResponse, string? errorMessage, DateTimeOffset attemptedAt)
    {
        AssignWorkspace(workspaceId);
        SendMailId = sendMailId;
        AttemptNo = attemptNo;
        Success = success;
        ProviderResponse = providerResponse;
        ErrorMessage = errorMessage;
        AttemptedAt = attemptedAt;
    }

    public long SendMailId { get; private set; }
    public int AttemptNo { get; private set; }
    public bool Success { get; private set; }
    public string? ProviderResponse { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }
}
