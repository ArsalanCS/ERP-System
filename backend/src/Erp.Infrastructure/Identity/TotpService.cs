using System.Security.Cryptography;
using System.Text;
using Erp.Application.Abstractions;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// RFC 6238 TOTP (SHA-1, 30s step, 6 digits) compatible with Google
/// Authenticator / Microsoft Authenticator / Authy. No external dependency.
/// </summary>
public sealed class TotpService(IClock clock) : ITotpService
{
    private const int StepSeconds = 30;
    private const int Digits = 6;
    private const int DriftSteps = 2; // accept ±2 windows (±60s) to tolerate phone clock drift

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20); // 160-bit secret
        return Base32Encode(bytes);
    }

    public string BuildOtpAuthUri(string secret, string accountName, string issuer)
    {
        var encIssuer = Uri.EscapeDataString(issuer);
        var encAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{encIssuer}:{encAccount}"
            + $"?secret={secret}&issuer={encIssuer}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();

        byte[] key;
        try
        {
            key = Base32Decode(secret);
        }
        catch
        {
            return false;
        }

        var counter = clock.UtcNow.ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -DriftSteps; offset <= DriftSteps; offset++)
        {
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(Compute(key, counter + offset)),
                    Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }
        return false;
    }

    private static string Compute(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString().PadLeft(Digits, '0');
    }

    // ---- Base32 (RFC 4648, no padding) -------------------------------------
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Alphabet[(buffer >> bitsLeft) & 0x1f]);
            }
        }
        if (bitsLeft > 0)
        {
            sb.Append(Alphabet[(buffer << (5 - bitsLeft)) & 0x1f]);
        }
        return sb.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        input = input.TrimEnd('=').ToUpperInvariant().Replace(" ", "");
        var output = new List<byte>(input.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var c in input)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0) throw new FormatException("Invalid Base32 character.");
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xff));
            }
        }
        return [.. output];
    }
}
