using Erp.Application.AccessControl;
using Erp.Application.Auth;
using Erp.Application.Structure;
using Erp.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Application;

/// <summary>Registers Application-layer services and validators.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IStructureService, StructureService>();
        services.AddScoped<Auditing.IAuditQueryService, Auditing.AuditQueryService>();
        services.AddScoped<Account.IAccountService, Account.AccountService>();
        services.AddScoped<Dashboard.IDashboardService, Dashboard.DashboardService>();
        services.AddScoped<Settings.ISettingsService, Settings.SettingsService>();
        services.AddScoped<Security.ISecurityPolicyService, Security.SecurityPolicyService>();

        return services;
    }
}
