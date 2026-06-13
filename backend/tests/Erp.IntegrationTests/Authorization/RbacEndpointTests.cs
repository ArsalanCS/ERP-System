using System.Net.Http.Json;
using Erp.Api.Controllers;
using Erp.Application.Auth;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Authorization;

/// <summary>
/// End-to-end: a user's effective permissions are resolved server-side and
/// embedded in the access token, so the frontend (and authz filters) can read
/// them. Verifies the owner role yields the high-risk admin actions.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RbacEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public RbacEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Owner_user_token_carries_resolved_actions()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        await _factory.SeedOwnerUserAsync(slug, "owner@x.test", "Str0ngPass!");
        var client = _factory.CreateClient();

        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "owner@x.test", "Str0ngPass!"))).Content.ReadFromJsonAsync<AuthTokens>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new("Bearer", tokens!.AccessToken);
        var me = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(me);
        Assert.Contains("user.manage", me!.Actions);
        Assert.Contains("role.manage", me.Actions);
        Assert.Contains("audit.view", me.Actions);
    }
}
