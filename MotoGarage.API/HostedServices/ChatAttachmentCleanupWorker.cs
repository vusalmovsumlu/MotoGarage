using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.HostedServices;

public sealed class ChatAttachmentCleanupWorker(IServiceScopeFactory scopes, ILogger<ChatAttachmentCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IChatAttachmentService>().CleanupPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception failure) { logger.LogWarning("Chat media cleanup failed: {ErrorType}", failure.GetType().Name); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
