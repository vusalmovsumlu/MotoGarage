using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}