using Microsoft.Extensions.Options;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Infrastructure.Notifications;

namespace MotoGarage.API.HostedServices;

public sealed class ReminderNotificationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<ReminderNotificationOptions> options,
    ILogger<ReminderNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), timeProvider);
        try
        {
            do
            {
                try
                {
                    for (var batch = 0; batch < options.Value.MaxBatchesPerPass; batch++)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await using var scope = scopeFactory.CreateAsyncScope();
                        var dispatcher = scope.ServiceProvider
                            .GetRequiredService<IReminderNotificationDispatcher>();
                        var sent = await dispatcher.DispatchDueAsync(stoppingToken);
                        if (sent > 0)
                            logger.LogInformation("Created {Count} reminder notifications.", sent);
                        if (sent < options.Value.BatchSize)
                            break;
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception failure)
                {
                    logger.LogError(failure, "Reminder notification check failed; retrying on the next interval.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }
}
