namespace MotoGarage.Application.DTOs.Chat;

public class ConversationDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? MechanicId { get; set; }

    public int? StoreId { get; set; }

    public string UserFullName { get; set; } = "";
    public string? MechanicName { get; set; }
    public string? StoreName { get; set; }
    public string ParticipantName { get; set; } = "";
    public string Slug { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }
}
