namespace MotoGarage.Application.DTOs.Review;

public class ReviewDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserFullName { get; set; } = null!;

    public int MechanicId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
