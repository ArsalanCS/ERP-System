using System.Net;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Auth;

/// <summary>
/// End-to-end auth tests against the real API + PostgreSQL (Identity spec §11,
/// QA scope §15): login success/failure/lockout, refresh rotation + reuse →
/// revoke-all, tampered JWT → 401.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public AuthEndpointsTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static string Slug() => $"ws-{Guid.NewGuid():N}".ToLowerInvariant()[..16];
    private const string Password = "Str0ngPass!";

    [Fact]
    public async Task Login_with_valid_credentials_returns_tokens()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "a@x.test", Password);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "a@x.test", Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrWhiteSpace(tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_generic()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "b@x.test", Password);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "b@x.test", "WrongPass1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Account_locks_after_five_failed_attempts()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "c@x.test", Password);
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, "c@x.test", "WrongPass1!"));
        }

        // Even the correct password is now rejected (locked, 403).
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "c@x.test", Password));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_authentication_and_works_with_access_token()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "d@x.test", Password);
        var client = _factory.CreateClient();

        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "d@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new("Bearer", login!.AccessToken);
        var me = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Tampered_jwt_is_rejected_with_401()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "e@x.test", Password);
        var client = _factory.CreateClient();

        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "e@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();

        // Flip the last character of the signature segment.
        var tampered = login!.AccessToken[..^1] + (login.AccessToken[^1] == 'a' ? 'b' : 'a');
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new("Bearer", tampered);
        var me = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_reuse_revokes_all_sessions()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "f@x.test", Password);
        var client = _factory.CreateClient();

        var first = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "f@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();

        // Rotate once — succeeds, returns a new refresh token.
        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(first!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var secondTokens = await rotated.Content.ReadFromJsonAsync<AuthTokens>();

        // Re-using the ORIGINAL (now rotated) token is theft → 401 and revoke-all.
        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(first.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // The previously-valid rotated token is now also revoked.
        var afterRevoke = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(secondTokens!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_returns_202_without_revealing_account()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "g@x.test", Password);
        var client = _factory.CreateClient();

        var existing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest(slug, "g@x.test"));
        var missing = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest(slug, "nobody@x.test"));

        Assert.Equal(HttpStatusCode.Accepted, existing.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
    }
}
