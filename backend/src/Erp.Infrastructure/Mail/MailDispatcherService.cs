using Erp.Application.Abstractions;
using Erp.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Mail;

/// <summary>
/// Timer that periodically drains the mail outbox. Each tick opens a DI scope, establishes a
/// platform-admin tenant scope (so the trusted server-side job can dispatch across all
/// workspaces; RLS is bypassed only here), and delegates to <see cref="IMailDispatcher"/>.
/// </summary>
public sealed class MailDispatcherService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<MailDispatcherService> logger) : BackgroundService
{
    private TimeSpan PollInterval => TimeSpan.FromSeconds(
        int.TryParse(config["Mail:Dispatcher:PollSeconds"], out var s) && s > 0 ? s : 15);
    private int BatchSize => int.TryParse(config["Mail:Dispatcher:BatchSize"], out var b) && b > 0 ? b : 25;
    private bool Enabled => !bool.TryParse(config["Mail:Dispatcher:Enabled"], out var e) || e; // default on

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            logger.LogInformation("Mail dispatcher disabled via configuration.");
            return;
        }

        logger.LogInformation("Mail dispatcher started (poll every {Seconds}s, batch {Batch}).",
            PollInterval.TotalSeconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                tenant.SetScope(null, [], isPlatformAdmin: true);
                _ = scope.ServiceProvider.GetRequiredService<ErpDbContext>(); // ensure the scoped context shares the tenant scope
                var dispatcher = scope.ServiceProvider.GetRequiredService<IMailDispatcher>();
                await dispatcher.DispatchDueAsync(BatchSize, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Mail dispatcher tick failed.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
