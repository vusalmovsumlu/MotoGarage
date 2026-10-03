namespace MotoGarage.Application.DTOs.Review;

public class CreateReviewDto
{
    public int MechanicId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }
}