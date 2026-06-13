using Erp.Domain.Identity;

namespace Erp.Application.Abstractions;

/// <summary>Issues signed JWT access tokens (RS256) for authenticated users.</summary>
public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<string> actions, IReadOnlyCollection<Guid> clusterIds, bool isPlatformAdmin);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
