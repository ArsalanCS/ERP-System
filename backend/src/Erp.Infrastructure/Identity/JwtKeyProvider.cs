using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// Holds the RS256 signing key (CLAUDE.md §4.5). In production the RSA-2048
/// private key is loaded from configuration (sourced from AWS Secrets Manager,
/// never from source — §4.4). For dev/test an ephemeral key is generated per
/// process. Exposes the public key for the JWKS endpoint.
/// </summary>
public interface IJwtKeyProvider
{
    string KeyId { get; }
    SigningCredentials SigningCredentials { get; }
    SecurityKey PublicSigningKey { get; }
    object GetJwks();
}

public sealed class JwtKeyProvider : IJwtKeyProvider, IDisposable
{
    private readonly RSA _rsa;
    private readonly RsaSecurityKey _key;

    public JwtKeyProvider(IConfiguration configuration)
    {
        _rsa = RSA.Create(2048);

        var pem = configuration["Jwt:PrivateKeyPem"];
        if (!string.IsNullOrWhiteSpace(pem))
        {
            _rsa.ImportFromPem(pem);
        }
        // else: ephemeral dev/test key (already created above).

        KeyId = Convert.ToHexStringLower(SHA256.HashData(_rsa.ExportRSAPublicKey()))[..16];
        _key = new RsaSecurityKey(_rsa) { KeyId = KeyId };
        SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);
    }

    public string KeyId { get; }
    public SigningCredentials SigningCredentials { get; }
    public SecurityKey PublicSigningKey => _key;

    /// <summary>JWKS document model exposed at /.well-known/jwks.json.</summary>
    public object GetJwks()
    {
        var parameters = _rsa.ExportParameters(includePrivateParameters: false);
        return new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = KeyId,
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent),
                },
            },
        };
    }

    public void Dispose() => _rsa.Dispose();
}
