using Erp.Application.Abstractions;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against the dedicated test PostgreSQL database so auth
/// flows run end-to-end (RLS active). Provides a seeding helper.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("ERP_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=erp_test;Username=erp;Password=erp_local_dev";

    /// <summary>Captures token-bearing emails so token flows can be driven in tests.</summary>
    public TestEmailSender Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // UseSetting values are visible to builder-time configuration reads in
        // Program.cs (unlike ConfigureAppConfiguration, which applies too late).
        builder.UseSetting("ConnectionStrings:Postgres", _connectionString);
        // Tests share one client IP; raise the auth rate limit so parallel tests
        // don't trip it (the limit itself is covered by its own dedicated test).
        builder.UseSetting("RateLimit:Auth:PermitLimit", "100000");
        // Drive the mail dispatcher deterministically from tests (DispatchMailAsync)
        // instead of letting the timer fire mid-test.
        builder.UseSetting("Mail:Dispatcher:Enabled", "false");

        // Swap the real email sender for a capturing double — raw tokens are never
        // returned by the API, so this is the only way tests can read them.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
        });
    }

    /// <summary>Ensures the schema exists (idempotent).</summary>
    public async Task EnsureMigratedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Seeds an active workspace + user with a known password.</summary>
    public async Task<(long workspaceId, long userId)> SeedActiveUserAsync(string slug, string email, string password)
    {
        using var scope = Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(null, [], isPlatformAdmin: true);

        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var workspace = new Workspace($"WS {slug}", slug, "en", "Asia/Riyadh", "SAR");
        workspace.Activate();
        db.Workspaces.Add(workspace);

        var user = new User(workspace.Id, email, "Test", "User");
        user.SetPasswordHash(hasher.Hash(password));
        user.Activate();
        db.Users.Add(user);

        await db.SaveChangesAsync();
        return (workspace.Id, user.Id);
    }

    /// <summary>Seeds an active user assigned the Workspace Owner role (all permissions).</summary>
    public async Task<(long workspaceId, long userId)> SeedOwnerUserAsync(string slug, string email, string password)
    {
        var (workspaceId, userId) = await SeedActiveUserAsync(slug, email, password);

        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<Erp.Infrastructure.Persistence.Seeding.IIdentitySeeder>();
        var role = await seeder.EnsureWorkspaceOwnerRoleAsync(workspaceId);

        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(workspaceId, [], isPlatformAdmin: true);
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        db.UserRoles.Add(new Erp.Domain.Authorization.UserRole(workspaceId, userId, role.Id));
        await db.SaveChangesAsync();

        // Make the workspace task-ready (global catalogs + default statuses), mirroring provisioning.
        await seeder.SeedEventAssetCatalogAsync();
        await seeder.SeedMailTemplatesCatalogAsync();
        await seeder.SeedDefaultStatusesAsync(workspaceId);

        return (workspaceId, userId);
    }

    /// <summary>Seeds an additional active user (with an email) into an existing workspace.</summary>
    public async Task<long> SeedExtraUserAsync(long workspaceId, string email, string displayFirst = "Extra")
    {
        using var scope = Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(workspaceId, [], isPlatformAdmin: true);

        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var user = new User(workspaceId, email, displayFirst, "User");
        user.Activate();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>Deterministically drains the mail outbox (the timer worker is disabled in tests).</summary>
    public async Task<int> DispatchMailAsync()
    {
        using var scope = Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(null, [], isPlatformAdmin: true);
        var dispatcher = scope.ServiceProvider.GetRequiredService<Erp.Application.Abstractions.IMailDispatcher>();
        return await dispatcher.DispatchDueAsync(ct: default);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
