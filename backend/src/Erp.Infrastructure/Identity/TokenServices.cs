using System.Security.Cryptography;
using Erp.Application.Abstractions;

namespace Erp.Infrastructure.Identity;

/// <summary>SHA-256 hashing for opaque tokens (refresh, password-reset) at rest.</summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }
}

/// <summary>Cryptographically-strong URL-safe random token generator.</summary>
public sealed class RandomTokenGenerator : ITokenGenerator
{
    public string NewSecret(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Base64UrlEncode(buffer);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
