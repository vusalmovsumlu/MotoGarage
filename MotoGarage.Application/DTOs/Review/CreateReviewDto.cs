using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Review;

public class CreateReviewDto
{
    public int MechanicId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}