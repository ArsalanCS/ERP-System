using Erp.Application.Abstractions;
using Erp.Domain.Identity;
using Erp.Domain.Tenancy;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // UseSetting values are visible to builder-time configuration reads in
        // Program.cs (unlike ConfigureAppConfiguration, which applies too late).
        builder.UseSetting("ConnectionStrings:Postgres", _connectionString);
        // Tests share one client IP; raise the auth rate limit so parallel tests
        // don't trip it (the limit itself is covered by its own dedicated test).
        builder.UseSetting("RateLimit:Auth:PermitLimit", "100000");
    }

    /// <summary>Ensures the schema exists (idempotent).</summary>
    public async Task EnsureMigratedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Seeds an active workspace + user with a known password.</summary>
    public async Task<(Guid workspaceId, Guid userId)> SeedActiveUserAsync(string slug, string email, string password)
    {
        using var scope = Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetScope(Guid.Empty, [], isPlatformAdmin: true);

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
    public async Task<(Guid workspaceId, Guid userId)> SeedOwnerUserAsync(string slug, string email, string password)
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

        return (workspaceId, userId);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
