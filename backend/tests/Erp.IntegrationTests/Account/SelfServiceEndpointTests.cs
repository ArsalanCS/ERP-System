using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Account;
using Erp.Application.Auth;
using Erp.Application.Dashboard;
using Erp.Application.Settings;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Account;

[Collection(ApiCollection.Name)]
public sealed class SelfServiceEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public SelfServiceEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string Password = "Str0ngPass!";

    private async Task<HttpClient> OwnerClientAsync(string slug, string email)
    {
        await _factory.SeedOwnerUserAsync(slug, email, Password);
        var client = _factory.CreateClient();
        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, Password))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Profile_dashboard_and_settings_are_reachable_for_owner()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug, "self@x.test");

        var profile = await client.GetFromJsonAsync<MyProfileDto>("/api/v1/me/profile");
        Assert.Equal("self@x.test", profile!.Email);

        var summary = await client.GetFromJsonAsync<DashboardSummary>("/api/v1/admin/overview");
        Assert.True(summary!.TotalUsers >= 1);
        Assert.True(summary.ActiveUsers >= 1);

        var settings = await client.GetFromJsonAsync<WorkspaceSettingsDto>("/api/v1/settings");
        Assert.Equal(slug, settings!.Slug);
    }

    [Fact]
    public async Task Change_password_requires_correct_current_password()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug, "pw@x.test");

        var wrong = await client.PostAsJsonAsync("/api/v1/me/change-password",
            new ChangePasswordRequest("WrongCurrent1!", "NewStr0ng!"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);

        var ok = await client.PostAsJsonAsync("/api/v1/me/change-password",
            new ChangePasswordRequest(Password, "NewStr0ng1!"));
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
    }

    [Fact]
    public async Task Sessions_lists_the_active_login_session()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        var client = await OwnerClientAsync(slug, "sess@x.test");

        var sessions = await client.GetFromJsonAsync<List<SessionDto>>("/api/v1/me/sessions");
        Assert.NotNull(sessions);
        Assert.NotEmpty(sessions!);
    }
}
