namespace MotoGarage.Application.DTOs.Chat;

public class ConversationDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? MechanicId { get; set; }

    public int? StoreId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }
}