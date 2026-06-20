using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Structure;
using Erp.Application.Users;
using Erp.Domain.Structure;
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

    private static async Task<Guid> CreateNode(HttpClient client, Guid? parent, StructureNodeType type, string name, string code)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new CreateNodeRequest(parent, type, name, code, null, null, null));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<CreatedId>())!.Id;
    }

    [Fact]
    public async Task Owner_can_build_the_full_node_tree()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);
        var u = Guid.NewGuid().ToString("N")[..6];

        var org = await CreateNode(client, null, StructureNodeType.Organization, $"Org {u}", $"org-{u}");
        var dept = await CreateNode(client, org, StructureNodeType.Department, $"Finance {u}", $"dep-{u}");
        var branch = await CreateNode(client, dept, StructureNodeType.Branch, $"Riyadh {u}", $"br-{u}");
        var subDept = await CreateNode(client, branch, StructureNodeType.SubDepartment, $"AP {u}", $"sd-{u}");
        var team = await CreateNode(client, subDept, StructureNodeType.Team, $"AP Team {u}", $"tm-{u}");
        await CreateNode(client, team, StructureNodeType.SubTeam, $"AP Squad {u}", $"st-{u}");

        var tree = await client.GetFromJsonAsync<StructureTree>("/api/v1/structure/tree");
        Assert.NotNull(tree);
        Assert.Contains(tree!.Nodes, n => n.Id == org && n.NodeType == StructureNodeType.Organization && n.ParentId == null);
        Assert.Contains(tree.Nodes, n => n.Id == dept && n.ParentId == org);
        Assert.Contains(tree.Nodes, n => n.Id == branch && n.ParentId == dept);
        Assert.Contains(tree.Nodes, n => n.NodeType == StructureNodeType.SubTeam && n.ParentId == team);
    }

    [Fact]
    public async Task Organization_must_be_root_and_children_need_a_parent()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);
        var u = Guid.NewGuid().ToString("N")[..6];

        var org = await CreateNode(client, null, StructureNodeType.Organization, $"Org {u}", $"org-{u}");

        // Organization with a parent → rejected.
        var orgWithParent = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new CreateNodeRequest(org, StructureNodeType.Organization, "Bad", $"bad-{u}", null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, orgWithParent.StatusCode);

        // Non-organization without a parent → rejected.
        var deptNoParent = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new CreateNodeRequest(null, StructureNodeType.Department, "Bad", $"bad2-{u}", null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, deptNoParent.StatusCode);
    }

    [Fact]
    public async Task Node_with_children_cannot_be_archived()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);
        var u = Guid.NewGuid().ToString("N")[..6];

        var org = await CreateNode(client, null, StructureNodeType.Organization, $"Org {u}", $"org-{u}");
        await CreateNode(client, org, StructureNodeType.Department, $"Dept {u}", $"dep-{u}");

        var archive = await client.DeleteAsync($"/api/v1/structure/nodes/{org}");
        Assert.Equal(HttpStatusCode.Conflict, archive.StatusCode);
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

        var response = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new CreateNodeRequest(null, StructureNodeType.Organization, "X", "x-org", null, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Node_members_lists_users_placed_on_that_node()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug);
        var u = Guid.NewGuid().ToString("N")[..6];

        var org = await CreateNode(client, null, StructureNodeType.Organization, $"Org {u}", $"org-{u}");
        var dept = await CreateNode(client, org, StructureNodeType.Department, $"Dept {u}", $"dep-{u}");

        // Place a user on the department.
        var created = await (await client.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest($"member-{u}@x.test", "Mem", "Ber", null, "Analyst", "en", null,
                null, dept, null, null, null, SendInvitation: false)))
            .Content.ReadFromJsonAsync<CreateUserResult>();

        var members = await client.GetFromJsonAsync<List<StructureMemberDto>>(
            $"/api/v1/structure/nodes/{dept}/members");
        Assert.NotNull(members);
        Assert.Single(members!);
        Assert.Equal(created!.UserId, members![0].UserId);
        Assert.Equal("Analyst", members[0].JobTitle);

        // A sibling node with nobody placed has no members.
        var empty = await client.GetFromJsonAsync<List<StructureMemberDto>>(
            $"/api/v1/structure/nodes/{org}/members");
        Assert.Empty(empty!);
    }

    [Fact]
    public async Task Node_members_requires_view_permission()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password);
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "plain@x.test", Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var response = await client.GetAsync($"/api/v1/structure/nodes/{Guid.NewGuid()}/members");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
