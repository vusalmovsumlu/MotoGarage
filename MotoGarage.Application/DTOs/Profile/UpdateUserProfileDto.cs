namespace MotoGarage.Application.DTOs.Profile;

public class UpdateUserProfileDto
{
    public string FullName { get; set; } = null!;

    public string? PhoneNumber { get; set; }
}