namespace Erp.Application.Abstractions;

/// <summary>
/// Transactional email delivery (Identity spec §11). The token-bearing flows
/// (invitation, password reset) hand the raw token here; it is never returned
/// to the API caller. Production uses SES/SMTP; development writes to a local
/// outbox so the message is visible without a mail server.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an account invitation with a link to set a password and activate.</summary>
    Task SendInvitationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default);

    /// <summary>Sends a password-reset link.</summary>
    Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default);

    /// <summary>Sends an email-verification link to the owner of a newly self-registered workspace.</summary>
    Task SendEmailVerificationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default);
}
