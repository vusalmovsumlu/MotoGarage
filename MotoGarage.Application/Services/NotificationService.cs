using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Exceptions;
using AutoMapper;
using MotoGarage.Application.DTOs.Notification;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.Services;

public class NotificationService
    : INotificationService
{
    private readonly INotificationRepository
        _notificationRepository;

    private readonly IMapper _mapper;

    public NotificationService(
        INotificationRepository notificationRepository,
        IMapper mapper)
    {
        _notificationRepository =
            notificationRepository;

        _mapper = mapper;
    }

    public async Task<IEnumerable<NotificationDto>>
        GetMyNotificationsAsync(
            int userId,
            bool unreadOnly)
    {
        var notifications =
            await _notificationRepository
                .GetByUserIdAsync(
                    userId,
                    unreadOnly);

        return _mapper.Map<
            IEnumerable<NotificationDto>>(
                notifications);
    }

    public async Task<int> GetUnreadCountAsync(
        int userId)
    {
        return await _notificationRepository
            .GetUnreadCountAsync(userId);
    }

    public async Task CreateAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? link = null)
    {
        var notification =
            new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

        await _notificationRepository
            .AddAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(
        int notificationId,
        int userId)
    {
        var notification =
            await _notificationRepository
                .GetByIdAsync(notificationId);

        if (notification is null)
        {
            throw new KeyNotFoundException(
                "Bildiriş tapılmadı.");
        }

        if (notification.UserId != userId)
        {
            throw new ForbiddenException(
                "Bu bildiriş sənə aid deyil.");
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;

        await _notificationRepository
            .UpdateAsync(notification);

        await _notificationRepository
            .SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(
        int userId)
    {
        var notifications =
            await _notificationRepository
                .GetByUserIdAsync(
                    userId,
                    true);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;

            await _notificationRepository
                .UpdateAsync(notification);
        }

        await _notificationRepository
            .SaveChangesAsync();
    }
}