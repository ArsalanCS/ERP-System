using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Auditing;
using Erp.Application.Auth;
using Erp.Application.Common;
using Erp.Domain.Auditing;
using Erp.IntegrationTests.Infrastructure;
using Xunit;

namespace Erp.IntegrationTests.Auditing;

[Collection(ApiCollection.Name)]
public sealed class AuditQueryEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public AuditQueryEndpointTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Audit_search_returns_login_event_and_export_records_its_own_event()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        await _factory.SeedOwnerUserAsync(slug, "auditor@x.test", "Str0ngPass!");
        var client = _factory.CreateClient();

        var tokens = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, "auditor@x.test", "Str0ngPass!"))).Content.ReadFromJsonAsync<AuthTokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var search = await client.GetFromJsonAsync<PagedResult<AuditLogDto>>($"/api/v1/audit?action={AuditActions.Login}");
        Assert.NotNull(search);
        Assert.Contains(search!.Items, a => a.Action == AuditActions.Login);

        // Export, then confirm the export itself was audited.
        var export = await client.GetAsync("/api/v1/audit/export");
        export.EnsureSuccessStatusCode();

        var afterExport = await client.GetFromJsonAsync<PagedResult<AuditLogDto>>($"/api/v1/audit?action={AuditActions.Export}");
        Assert.Contains(afterExport!.Items, a => a.Action == AuditActions.Export);
    }
}
