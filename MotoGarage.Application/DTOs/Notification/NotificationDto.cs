using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Notification;

public class NotificationDto
{
    public int Id { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}