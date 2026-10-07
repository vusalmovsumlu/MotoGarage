using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Profile;

public class UpdateUserProfileDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}