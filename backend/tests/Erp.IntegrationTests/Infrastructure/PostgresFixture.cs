using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Interceptors;
using Erp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>
/// Shared real-PostgreSQL fixture for tenant-isolation tests. Connects to a
/// dedicated test database (RLS behaves identically to RDS) and applies
/// migrations once. Override the target with the <c>ERP_TEST_CONNECTION</c> env
/// var (e.g. in CI).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ERP_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=erp_test;Username=erp;Password=erp_local_dev";

    public async Task InitializeAsync()
    {
        // Migrate using a platform-admin scope (DDL is unaffected by RLS, but the
        // interceptor still needs a context).
        await using var context = CreateContext(out _, platformAdmin: true);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a context scoped to <paramref name="workspaceId"/> (or platform
    /// admin). The returned <see cref="TenantContext"/> can be re-scoped per test.
    /// </summary>
    public ErpDbContext CreateContext(out TenantContext tenant, Guid? workspaceId = null, bool platformAdmin = false)
    {
        tenant = new TenantContext();
        if (platformAdmin)
        {
            tenant.SetScope(Guid.Empty, [], isPlatformAdmin: true);
        }
        else if (workspaceId is { } id)
        {
            tenant.SetScope(id, []);
        }

        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var currentUser = new FakeCurrentUser { IsPlatformAdmin = platformAdmin };

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new RlsConnectionInterceptor(tenant),
                new AuditSaveChangesInterceptor(clock, currentUser, tenant))
            .Options;

        return new ErpDbContext(options, tenant);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
