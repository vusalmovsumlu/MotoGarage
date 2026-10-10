using Microsoft.Extensions.Options;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Infrastructure.Notifications;

namespace MotoGarage.API.HostedServices;

public sealed class AppointmentEmailReminderWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    IOptions<AppointmentEmailReminderOptions> options,
    ILogger<AppointmentEmailReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), clock);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var sent = await scope.ServiceProvider.GetRequiredService<IAppointmentEmailReminderDispatcher>().DispatchDueAsync(stoppingToken);
                    if (sent > 0) logger.LogInformation("Sent {Count} appointment email reminders.", sent);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception failure)
                {
                    logger.LogError(failure, "Appointment email reminder check failed; retrying on the next interval.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
