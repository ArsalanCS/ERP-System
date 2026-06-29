using Microsoft.Extensions.DependencyInjection;

namespace Erp.Application.Tasks;

/// <summary>Registers Task Management (Event/Asset) application services.</summary>
public static class TaskModule
{
    public static IServiceCollection Register(IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskSettingsService, TaskSettingsService>();
        return services;
    }
}
