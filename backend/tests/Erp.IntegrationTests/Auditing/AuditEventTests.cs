using System.Net.Http.Json;
using Erp.Application.Auth;
using Erp.Domain.Auditing;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Erp.IntegrationTests.Auditing;

/// <summary>
/// Verifies required audit events are written for auth flows (CLAUDE.md §4.3:
/// LOGIN / FAILED_LOGIN) and that no secrets are recorded.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuditEventTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    public AuditEventTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => _factory.EnsureMigratedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_success_and_failure_write_audit_entries_without_secrets()
    {
        var slug = $"ws-{Guid.NewGuid():N}"[..16];
        const string password = "Str0ngPass!";
        var (workspaceId, _) = await _factory.SeedActiveUserAsync(slug, "audit@x.test", password);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, "audit@x.test", "WrongPass1!"));
        await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(slug, "audit@x.test", password));

        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(workspaceId, [], isPlatformAdmin: true);
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        var logs = await db.AuditLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .ToListAsync();

        Assert.Contains(logs, a => a.Action == AuditActions.Login && a.Result == AuditResult.Success);
        Assert.Contains(logs, a => a.Action == AuditActions.FailedLogin && a.Result == AuditResult.Failed);

        // No secrets/passwords captured in any audit field.
        Assert.All(logs, a =>
        {
            Assert.DoesNotContain(password, a.NewValues ?? string.Empty);
            Assert.DoesNotContain(password, a.OldValues ?? string.Empty);
            Assert.DoesNotContain(password, a.Reason ?? string.Empty);
        });
    }
}
