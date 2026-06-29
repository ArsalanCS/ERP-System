using Erp.Application.Abstractions;
using Erp.Application.Auth;
using Erp.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Erp.Api.Security;

/// <summary>
/// Configures JWT bearer authentication (RS256) and enforces the security-stamp
/// check on every authenticated request so that password resets, suspensions,
/// and critical role changes revoke live access tokens immediately
/// (Identity spec §7.2; CLAUDE.md §4.5).
/// </summary>
public static class AuthenticationSetup
{
    public static IServiceCollection AddErpAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configure JwtBearer via the options pattern so the SAME singleton
        // IJwtKeyProvider (and thus the same signing key) is used to validate.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IJwtKeyProvider>((options, keyProvider) =>
            {
                var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = keyProvider.PublicSigningKey,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateSecurityStampAsync,
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static async Task ValidateSecurityStampAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var stampClaim = principal?.FindFirst(ErpClaimTypes.SecurityStamp)?.Value;
        var userIdValue = principal?.FindFirst(ErpClaimTypes.UserId)?.Value;
        var workspaceValue = principal?.FindFirst(ErpClaimTypes.WorkspaceId)?.Value;

        if (stampClaim is null || !long.TryParse(userIdValue, out var userId) || !long.TryParse(workspaceValue, out var workspaceId))
        {
            context.Fail("Malformed token.");
            return;
        }

        var services = context.HttpContext.RequestServices;
        var tenant = services.GetRequiredService<ITenantContext>();
        tenant.SetScope(workspaceId, []);

        var users = services.GetRequiredService<IUserRepository>();
        var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

        if (user is null || !string.Equals(user.SecurityStamp, stampClaim, StringComparison.Ordinal))
        {
            context.Fail("Security stamp mismatch — the session has been revoked.");
        }
    }
}
