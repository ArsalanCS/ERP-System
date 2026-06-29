using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.AccessControl;
using Erp.Application.Auth;
using Erp.Domain.Authorization;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.AccessControl;

[Collection(ApiCollection.Name)]
public sealed class AccessControlEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public AccessControlEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";

    private async Task<HttpClient> OwnerClientAsync(string slug)
    {
        await _factory.SeedOwnerUserAsync(slug, "owner@x.test", Password);
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "owner@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Owner_can_create_a_role_and_set_its_permission_matrix()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);

        var permissions = await client.GetFromJsonAsync<List<PermissionDto>>("/api/v1/permissions");
        Assert.NotNull(permissions);
        var userView = permissions!.First(p => p.Code == PermissionCatalog.UserView);

        var create = await client.PostAsJsonAsync("/api/v1/roles",
            new CreateRoleRequest("Support Agent", $"support-{Guid.NewGuid():N}"[..16], "Read-only support", "#3b6ea5"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var roleId = (await create.Content.ReadFromJsonAsync<CreatedId>())!.Id;

        var setPerms = await client.PutAsJsonAsync($"/api/v1/roles/{roleId}/permissions",
            new SetRolePermissionsRequest([new PermissionGrant(userView.Id, DataScope.Workspace)]));
        Assert.Equal(HttpStatusCode.NoContent, setPerms.StatusCode);

        var detail = await client.GetFromJsonAsync<RoleDetail>($"/api/v1/roles/{roleId}");
        Assert.NotNull(detail);
        Assert.Contains(detail!.Permissions, p => p.Code == PermissionCatalog.UserView);
    }

    [Fact]
    public async Task Role_management_requires_permission()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "plain@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/roles",
            new CreateRoleRequest("X", "x-role", null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record CreatedId(long Id);
}
