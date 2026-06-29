using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Application.Users;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Users;

/// <summary>
/// Users page API (Identity spec §4) — authorization, tenant isolation, and CRUD.
/// Covers the CONVENTIONS-required authz + cross-tenant negative cases.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class UsersEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public UsersEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";
    private static string Slug() => $"ws-{Guid.NewGuid():N}"[..16];

    private async Task<HttpClient> LoginAsync(string slug, string email)
    {
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Owner_can_list_and_create_users()
    {
        var slug = Slug();
        await _factory.SeedOwnerUserAsync(slug, "owner@x.test", Password);
        var client = await LoginAsync(slug, "owner@x.test");

        var created = await client.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest("newbie@x.test", "New", "Bie", null, "Analyst", "en", null,
                null, null, null, null, null, SendInvitation: true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<PagedResult<UserListItem>>("/api/v1/users");
        Assert.NotNull(list);
        Assert.Contains(list!.Items, u => u.Email == "newbie@x.test");
    }

    [Fact]
    public async Task User_without_permission_gets_403()
    {
        var slug = Slug();
        await _factory.SeedActiveUserAsync(slug, "plain@x.test", Password); // no roles
        var client = await LoginAsync(slug, "plain@x.test");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_gets_401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_read_a_user_from_another_workspace()
    {
        // Workspace A owner.
        var slugA = Slug();
        await _factory.SeedOwnerUserAsync(slugA, "owner-a@x.test", Password);
        // Workspace B with its own user.
        var slugB = Slug();
        var (_, userBId) = await _factory.SeedActiveUserAsync(slugB, "victim-b@x.test", Password);

        var clientA = await LoginAsync(slugA, "owner-a@x.test");

        // A's token must not be able to fetch B's user — RLS + query filter hide it → 404.
        var response = await clientA.GetAsync($"/api/v1/users/{userBId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Created_user_can_be_fetched_by_id()
    {
        var slug = Slug();
        await _factory.SeedOwnerUserAsync(slug, "owner2@x.test", Password);
        var client = await LoginAsync(slug, "owner2@x.test");

        var created = await (await client.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest("fetch@x.test", "Fetch", "Me", null, null, "en", null,
                null, null, null, null, null, false)))
            .Content.ReadFromJsonAsync<CreateUserResult>();

        var detail = await client.GetFromJsonAsync<UserDetail>($"/api/v1/users/{created!.UserId}");
        Assert.NotNull(detail);
        Assert.Equal("fetch@x.test", detail!.Email);
    }

    [Fact]
    public async Task Invite_with_placement_creates_employee_with_details_and_node()
    {
        var slug = Slug();
        await _factory.SeedOwnerUserAsync(slug, "hr@x.test", Password);
        var client = await LoginAsync(slug, "hr@x.test");
        var u = Guid.NewGuid().ToString("N")[..6];

        // Build a placement node (org → department).
        var orgResp = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new Erp.Application.Structure.CreateNodeRequest(
                null, Erp.Domain.Structure.StructureNodeType.Organization, $"Org {u}", $"org-{u}", null, null, null));
        var orgId = (await orgResp.Content.ReadFromJsonAsync<IdHolder>())!.Id;
        var deptResp = await client.PostAsJsonAsync("/api/v1/structure/nodes",
            new Erp.Application.Structure.CreateNodeRequest(
                orgId, Erp.Domain.Structure.StructureNodeType.Department, $"Dept {u}", $"dep-{u}", null, null, null));
        var deptId = (await deptResp.Content.ReadFromJsonAsync<IdHolder>())!.Id;

        // Invite a user placed in that department, with HR details.
        var created = await (await client.PostAsJsonAsync("/api/v1/users",
            new CreateUserRequest("placed@x.test", "Placed", "Person", "+966500000000", "Accountant", "en", null,
                "EMP-100", deptId, null, null, null, SendInvitation: false)))
            .Content.ReadFromJsonAsync<CreateUserResult>();

        var detail = await client.GetFromJsonAsync<UserDetail>($"/api/v1/users/{created!.UserId}");
        Assert.NotNull(detail);
        Assert.Equal("Accountant", detail!.JobTitle);
        Assert.Equal("+966500000000", detail.Mobile);
        Assert.Equal("EMP-100", detail.EmployeeNumber);
        Assert.Equal(deptId, detail.PlacementNodeId);
    }

    private sealed record IdHolder(long Id);
}
