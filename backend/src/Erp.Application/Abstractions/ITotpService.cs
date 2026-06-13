namespace Erp.Application.Abstractions;

/// <summary>
/// Time-based one-time password (RFC 6238) for authenticator-app 2FA
/// (Identity spec §7). Used for enrollment and login verification.
/// </summary>
public interface ITotpService
{
    /// <summary>Generates a new Base32-encoded shared secret.</summary>
    string GenerateSecret();

    /// <summary>
    /// Builds the otpauth:// URI an authenticator app encodes as a QR code.
    /// </summary>
    string BuildOtpAuthUri(string secret, string accountName, string issuer);

    /// <summary>
    /// Verifies a 6-digit code against the secret, allowing a ±1 step (30s) drift.
    /// </summary>
    bool VerifyCode(string secret, string code);
}
