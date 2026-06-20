using System.Collections.Concurrent;
using Erp.Application.Abstractions;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IEmailSender"/> that captures the raw token of the
/// most recent message per recipient, so token-bearing flows (invitation, reset,
/// email verification) can be driven end-to-end without a mail server. The raw
/// token is never returned by the API, so this is how tests obtain it.
/// </summary>
public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _verification = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _reset = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _invitation = new(StringComparer.OrdinalIgnoreCase);

    public Task SendInvitationAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        _invitation[toEmail] = token;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        _reset[toEmail] = token;
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        _verification[toEmail] = token;
        return Task.CompletedTask;
    }

    public string? VerificationTokenFor(string email) => _verification.GetValueOrDefault(email);
    public string? ResetTokenFor(string email) => _reset.GetValueOrDefault(email);
    public string? InvitationTokenFor(string email) => _invitation.GetValueOrDefault(email);
}
