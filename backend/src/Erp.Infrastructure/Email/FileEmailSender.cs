using Erp.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Email;

/// <summary>
/// Development email sender. Writes each message to a local outbox folder as an
/// HTML file and logs the action link, so invitation / reset flows are fully
/// usable without an SMTP/SES server. Swap for an SES implementation in prod
/// (the <see cref="IEmailSender"/> abstraction stays the same).
/// </summary>
public sealed class FileEmailSender(IConfiguration config, ILogger<FileEmailSender> logger) : IEmailSender
{
    private string WebBaseUrl => (config["App:WebBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
    private string OutboxPath => config["Email:OutboxPath"]
        ?? Path.Combine(Directory.GetCurrentDirectory(), "logs", "mail");

    public Task SendInvitationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default)
    {
        var url = $"{WebBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return WriteAsync(toEmail, "You're invited to the workspace",
            $"Hello {displayName}, an account has been created for you. "
            + "Click the button below to set your password and sign in.",
            "Set your password", url, cancellationToken);
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default)
    {
        var url = $"{WebBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return WriteAsync(toEmail, "Reset your password",
            $"Hello {displayName}, we received a request to reset your password. "
            + "Click the button below to choose a new one. If you didn't request this, ignore this email.",
            "Reset password", url, cancellationToken);
    }

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string token, CancellationToken cancellationToken = default)
    {
        var url = $"{WebBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        return WriteAsync(toEmail, "Confirm your email to activate your workspace",
            $"Hello {displayName}, thanks for signing up. "
            + "Confirm your email address with the button below to activate your workspace, then sign in.",
            "Confirm email", url, cancellationToken);
    }

    private async Task WriteAsync(string toEmail, string subject, string body, string cta, string url, CancellationToken ct)
    {
        Directory.CreateDirectory(OutboxPath);
        var file = Path.Combine(OutboxPath, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Sanitize(toEmail)}.html");
        var html = $"""
            <!doctype html><html><head><meta charset="utf-8"><title>{subject}</title></head>
            <body style="font-family:system-ui,sans-serif;background:#f7f6f3;padding:32px;color:#1c1a17">
              <div style="max-width:520px;margin:auto;background:#fff;border:1px solid #e4e0d8;border-radius:12px;padding:28px">
                <h1 style="font-size:20px;margin:0 0 6px">{subject}</h1>
                <p style="color:#6e675b;font-size:14px;line-height:1.5">To: {toEmail}</p>
                <p style="font-size:15px;line-height:1.6">{body}</p>
                <p style="margin:24px 0">
                  <a href="{url}" style="background:#c96442;color:#fff;text-decoration:none;
                     padding:11px 20px;border-radius:8px;font-weight:600;display:inline-block">{cta}</a>
                </p>
                <p style="color:#928b7d;font-size:12.5px;word-break:break-all">Or paste this link: {url}</p>
              </div>
            </body></html>
            """;
        await File.WriteAllTextAsync(file, html, ct);

        // Make the link obvious in the console/log for demos (dev only).
        logger.LogInformation("📧 Email '{Subject}' for {Email} written to {File}\n    link: {Url}",
            subject, toEmail, file, url);
    }

    private static string Sanitize(string s) =>
        string.Concat(s.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
