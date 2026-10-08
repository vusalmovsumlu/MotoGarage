namespace MotoGarage.Application.Interfaces.Services;

public interface IReminderNotificationDispatcher
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken = default);
}
