using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using MotoGarage.API.Hubs;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.HostedServices;

public sealed class NotificationChangeQueue : INotificationChangeQueue
{
    readonly Channel<int> channel = Channel.CreateBounded<int>(new BoundedChannelOptions(4096)
    { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest });
    public void Enqueue(IEnumerable<int> userIds)
    {
        foreach (var id in userIds.Distinct()) channel.Writer.TryWrite(id);
    }
    public ChannelReader<int> Reader => channel.Reader;
}

public sealed class NotificationRealtimeWorker(
    NotificationChangeQueue queue,
    IHubContext<ChatHub> hub,
    ILogger<NotificationRealtimeWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var userId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // No payload or client-supplied target: recipients fetch through
                // their existing authenticated, ownership-checked API.
                await hub.Clients.Group(ChatHub.GetUserGroup(userId))
                    .SendAsync("NotificationsChanged", cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception failure) { logger.LogWarning(failure, "Notification live update failed."); }
        }
    }
}
