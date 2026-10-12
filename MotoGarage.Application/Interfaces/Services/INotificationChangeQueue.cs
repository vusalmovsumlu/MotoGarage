namespace MotoGarage.Application.Interfaces.Services;

// A wake-up hint after persistence, never a replacement for stored notifications.
public interface INotificationChangeQueue
{
    void Enqueue(IEnumerable<int> userIds);
}
