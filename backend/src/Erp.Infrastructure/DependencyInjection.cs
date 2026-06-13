using Erp.Application.Abstractions;
using Erp.Infrastructure.Identity;
using Erp.Infrastructure.Persistence;
using Erp.Infrastructure.Persistence.Interceptors;
using Erp.Infrastructure.Persistence.Repositories;
using Erp.Infrastructure.Tenancy;
using Erp.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer. The API host calls
/// <see cref="AddInfrastructure"/> to register EF Core, the tenant context, the
/// RLS + audit interceptors, and supporting services.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionName = "Postgres";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();

        // One tenant context per request, surfaced as the scope holder.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<RlsConnectionInterceptor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString(DefaultConnectionName);

        services.AddDbContext<ErpDbContext>((sp, options) =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ErpDbContext).Assembly.FullName));
            }

            // Snake_case table/column names to match the spec's data model (§12.1:
            // workspace_id, created_by, is_deleted, …).
            options.UseSnakeCaseNamingConvention();

            options.AddInterceptors(
                sp.GetRequiredService<RlsConnectionInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        // Auth + identity services.
        services.AddSingleton<IJwtKeyProvider, JwtKeyProvider>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<ITotpService, TotpService>();
        // Real SMTP delivery when configured (Email:Smtp:Host set); otherwise a
        // local file outbox so dev flows still work without a mail server.
        if (!string.IsNullOrWhiteSpace(configuration["Email:Smtp:Host"]))
        {
            services.AddSingleton<IEmailSender, Email.SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, Email.FileEmailSender>();
        }
        services.AddSingleton<ITokenGenerator, RandomTokenGenerator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IPermissionResolver, Authorization.PermissionResolver>();
        services.AddScoped<IAuditLogger, Auditing.AuditLogger>();
        services.AddScoped<Persistence.Seeding.IIdentitySeeder, Persistence.Seeding.IdentitySeeder>();

        // Repositories + unit of work.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IStructureRepository, StructureRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<Application.Dashboard.IDashboardRepository, DashboardRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISecurityPolicyRepository, SecurityPolicyRepository>();

        return services;
    }
}
