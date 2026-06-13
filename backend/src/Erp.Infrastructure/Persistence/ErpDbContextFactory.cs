using Erp.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to create the context for
/// migration scaffolding. It does not connect to a database — the connection
/// string here is a placeholder; runtime config comes from the API host
/// (and ultimately AWS Secrets Manager, never source — CLAUDE.md §4.4).
/// Override via the <c>ERP_DESIGNTIME_CONNECTION</c> env var when applying.
/// </summary>
public sealed class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ERP_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=erp;Username=erp;Password=erp";

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(ErpDbContextFactory).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        // Design-time only: an empty tenant scope (migrations don't query data).
        return new ErpDbContext(options, new TenantContext());
    }
}
