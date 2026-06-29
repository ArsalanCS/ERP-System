using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Erp.Application.Abstractions;
using Erp.Application.Auth;
using Erp.Domain.Identity;
using Microsoft.Extensions.Options;

namespace Erp.Infrastructure.Identity;

/// <summary>Issues RS256-signed JWT access tokens with the platform claim set.</summary>
public sealed class JwtTokenService(
    IJwtKeyProvider keyProvider,
    IClock clock,
    IOptions<AuthOptions> options) : IJwtTokenService
{
    private readonly AuthOptions _options = options.Value;

    // Mirror of Erp.Api.Security.ErpClaimTypes (kept in sync; tokens are read there).
    private const string UserIdClaim = "uid";
    private const string WorkspaceIdClaim = "wsid";
    private const string EmailClaim = "email";
    private const string PlatformAdminClaim = "padmin";
    private const string ClusterClaim = "cluster";
    private const string ActionClaim = "act";
    private const string SecurityStampClaim = "sstamp";

    public AccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<string> actions,
        IReadOnlyCollection<long> clusterIds,
        bool isPlatformAdmin)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("n")),
            new(UserIdClaim, user.Id.ToString()),
            new(WorkspaceIdClaim, user.WorkspaceId.ToString()),
            new(EmailClaim, user.Email),
            new(SecurityStampClaim, user.SecurityStamp),
            new(PlatformAdminClaim, isPlatformAdmin ? "true" : "false"),
        };

        claims.AddRange(actions.Select(a => new Claim(ActionClaim, a)));
        claims.AddRange(clusterIds.Select(c => new Claim(ClusterClaim, c.ToString())));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: keyProvider.SigningCredentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, expires);
    }
}
