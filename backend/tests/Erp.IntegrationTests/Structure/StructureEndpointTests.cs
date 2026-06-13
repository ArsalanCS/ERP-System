using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Structure;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Structure;

[Collection(ApiCollection.Name)]
public sealed class StructureEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public StructureEndpointTests(ApiFactory factory) => _factory = factory;

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

    private sealed record CreatedId(Guid Id);

    [Fact]
    public async Task Owner_can_build_the_full_structure_tree()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);
        var u = Guid.NewGuid().ToString("N")[..6];

        var org = (await (await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest($"Org {u}", $"org-{u}", null, "Trading", null, null, "SA", "Riyadh", "SAR", null)))
            .Content.ReadFromJsonAsync<CreatedId>())!.Id;

        var cluster = (await (await client.PostAsJsonAsync("/api/v1/clusters",
            new CreateClusterRequest(org, $"Riyadh {u}", $"cl-{u}", "Branch", null, "Riyadh", null, null, true, true)))
            .Content.ReadFromJsonAsync<CreatedId>())!.Id;

        var dept = (await (await client.PostAsJsonAsync("/api/v1/departments",
            new CreateDepartmentRequest(org, cluster, $"Finance {u}", $"dep-{u}", null)))
            .Content.ReadFromJsonAsync<CreatedId>())!.Id;

        var teamResp = await client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest(dept, $"AP {u}", $"team-{u}", null));
        Assert.Equal(HttpStatusCode.Created, teamResp.StatusCode);

        var tree = await client.GetFromJsonAsync<StructureTree>("/api/v1/structure/tree");
        Assert.NotNull(tree);
        Assert.Contains(tree!.Organizations, o => o.Id == org);
        Assert.Contains(tree.Clusters, c => c.Id == cluster && c.OrganizationId == org);
        Assert.Contains(tree.Departments, d => d.Id == dept);
        Assert.Contains(tree.Teams, t => t.DepartmentId == dept);
    }

    [Fact]
    public async Task Structure_changes_require_permission()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "plain@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("X", "x-org", null, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
