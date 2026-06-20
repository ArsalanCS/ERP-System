using System.Net;
using System.Net.Mail;
using Erp.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Email;

/// <summary>
/// Real email delivery over SMTP (Gmail, SendGrid, Amazon SES SMTP, Mailtrap, …).
/// Used when <c>Email:Smtp:Host</c> is configured; otherwise the app falls back
/// to <see cref="FileEmailSender"/>. Credentials come from configuration /
/// environment variables, never from source (CLAUDE.md §4.4).
/// </summary>
public sealed class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private string Host => config["Email:Smtp:Host"]!;
    private int Port => int.TryParse(config["Email:Smtp:Port"], out var p) ? p : 587;
    private string? User => config["Email:Smtp:User"];
    private string? Password => config["Email:Smtp:Password"];
    private bool UseSsl => !bool.TryParse(config["Email:Smtp:UseSsl"], out var s) || s; // default true
    private string FromAddress => config["Email:Smtp:From"] ?? User ?? "no-reply@erp.local";
    private string FromName => config["Email:Smtp:FromName"] ?? "Xonfo ERP";
    private string WebBaseUrl => (config["App:WebBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    public Task SendInvitationAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        var url = $"{WebBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return SendAsync(toEmail, "You're invited to the workspace",
            $"Hello {displayName}, an account has been created for you. "
            + "Click the button below to set your password and sign in.",
            "Set your password", url, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        var url = $"{WebBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return SendAsync(toEmail, "Reset your password",
            $"Hello {displayName}, we received a request to reset your password. "
            + "Click the button below to choose a new one. If you didn't request this, ignore this email.",
            "Reset password", url, ct);
    }

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string token, CancellationToken ct = default)
    {
        var url = $"{WebBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        return SendAsync(toEmail, "Confirm your email to activate your workspace",
            $"Hello {displayName}, thanks for signing up. "
            + "Confirm your email address with the button below to activate your workspace, then sign in.",
            "Confirm email", url, ct);
    }

    private async Task SendAsync(string to, string subject, string body, string cta, string url, CancellationToken ct)
    {
        var html = $"""
            <!doctype html><html><body style="font-family:system-ui,sans-serif;background:#f7f6f3;padding:32px;color:#1c1a17">
              <div style="max-width:520px;margin:auto;background:#fff;border:1px solid #e4e0d8;border-radius:12px;padding:28px">
                <h1 style="font-size:20px;margin:0 0 10px">{subject}</h1>
                <p style="font-size:15px;line-height:1.6">{body}</p>
                <p style="margin:24px 0">
                  <a href="{url}" style="background:#c96442;color:#fff;text-decoration:none;
                     padding:11px 20px;border-radius:8px;font-weight:600;display:inline-block">{cta}</a>
                </p>
                <p style="color:#928b7d;font-size:12.5px;word-break:break-all">Or paste this link: {url}</p>
              </div>
            </body></html>
            """;

        using var msg = new MailMessage
        {
            From = new MailAddress(FromAddress, FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true,
        };
        msg.To.Add(to);

        using var client = new SmtpClient(Host, Port)
        {
            EnableSsl = UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrEmpty(User))
        {
            client.Credentials = new NetworkCredential(User, Password);
        }

        try
        {
            await client.SendMailAsync(msg, ct);
            logger.LogInformation("Sent '{Subject}' to {To} via {Host}:{Port}.", subject, to, Host, Port);
        }
        catch (Exception ex)
        {
            // Email is best-effort — never fail the surrounding operation (e.g. user
            // creation) because mail delivery failed. The link is logged so the
            // flow can still be completed and the misconfiguration diagnosed.
            logger.LogError(ex, "Failed to send '{Subject}' to {To} via {Host}:{Port}. Link: {Url}",
                subject, to, Host, Port, url);
        }
    }
}
