namespace Erp.Application.Abstractions;

/// <summary>
/// Hashes opaque tokens (refresh, password-reset) for at-rest storage. Only the
/// hash is persisted; the raw token is shown to the client once.
/// </summary>
public interface ITokenHasher
{
    string Hash(string rawToken);
}

/// <summary>Generates cryptographically-strong random tokens.</summary>
public interface ITokenGenerator
{
    /// <summary>A URL-safe random secret with at least the given byte strength.</summary>
    string NewSecret(int bytes = 32);
}
