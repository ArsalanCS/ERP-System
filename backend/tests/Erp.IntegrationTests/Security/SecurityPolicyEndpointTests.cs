using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Security;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Security;

[Collection(ApiCollection.Name)]
public sealed class SecurityPolicyEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public SecurityPolicyEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";

    private async Task<HttpClient> ClientFor(long _, string slug, string email, bool owner)
    {
        if (owner) await _factory.SeedOwnerUserAsync(slug, email, Password);
        else await _factory.SeedActiveUserAsync(slug, email, Password);

        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Owner_gets_default_policy_then_updates_it()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await ClientFor(0, slug, "sec@x.test", owner: true);

        var defaults = await client.GetFromJsonAsync<SecurityPolicyDto>("/api/v1/security/policy");
        Assert.NotNull(defaults);
        Assert.Equal(8, defaults!.PasswordMinLength);
        Assert.Equal(5, defaults.MaxFailedAttempts);
        Assert.False(defaults.RequireTwoFactor);

        var update = new UpdateSecurityPolicyRequest(
            PasswordMinLength: 12, RequireUppercase: true, RequireLowercase: true, RequireDigit: true,
            RequireSymbol: true, PasswordExpiryDays: 90, MaxFailedAttempts: 3, LockoutMinutes: 30,
            SessionIdleTimeoutMinutes: 45, RefreshTokenDays: 14, RequireTwoFactor: true);
        var put = await client.PutAsJsonAsync("/api/v1/security/policy", update);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var saved = await client.GetFromJsonAsync<SecurityPolicyDto>("/api/v1/security/policy");
        Assert.Equal(12, saved!.PasswordMinLength);
        Assert.Equal(3, saved.MaxFailedAttempts);
        Assert.True(saved.RequireTwoFactor);
        Assert.Equal(90, saved.PasswordExpiryDays);
    }

    [Fact]
    public async Task Non_privileged_user_is_forbidden()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await ClientFor(0, slug, "plain@x.test", owner: false);

        var res = await client.GetAsync("/api/v1/security/policy");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Validation_rejects_out_of_range_values()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await ClientFor(0, slug, "val@x.test", owner: true);

        var bad = new UpdateSecurityPolicyRequest(
            PasswordMinLength: 2, RequireUppercase: true, RequireLowercase: true, RequireDigit: true,
            RequireSymbol: false, PasswordExpiryDays: null, MaxFailedAttempts: 999, LockoutMinutes: 15,
            SessionIdleTimeoutMinutes: 60, RefreshTokenDays: 30, RequireTwoFactor: false);
        var res = await client.PutAsJsonAsync("/api/v1/security/policy", bad);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }
}
