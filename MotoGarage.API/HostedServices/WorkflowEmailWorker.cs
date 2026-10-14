using Microsoft.Extensions.Options;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Infrastructure.Email;

namespace MotoGarage.API.HostedServices;

public sealed class WorkflowEmailWorker(IServiceScopeFactory scopeFactory, TimeProvider clock,
    IOptions<WorkflowEmailOptions> options, ILogger<WorkflowEmailWorker> logger) : BackgroundService
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
                    var sent = await scope.ServiceProvider.GetRequiredService<IWorkflowEmailDispatcher>().DispatchDueAsync(stoppingToken);
                    if (sent > 0) logger.LogInformation("Sent {Count} workflow email updates.", sent);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception failure) { logger.LogError(failure, "Workflow email delivery failed; retrying on the next interval."); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
