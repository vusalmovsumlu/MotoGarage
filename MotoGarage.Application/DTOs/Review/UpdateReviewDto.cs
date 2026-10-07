using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Review;

public class UpdateReviewDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}