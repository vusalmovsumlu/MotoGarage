using MotoGarage.Application.DTOs.Notification;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.Interfaces.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>>
        GetMyNotificationsAsync(
            int userId,
            bool unreadOnly);

    Task<int> GetUnreadCountAsync(int userId);

    Task CreateAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? link = null);

    Task MarkAsReadAsync(
        int notificationId,
        int userId);

    Task MarkAllAsReadAsync(int userId);
}