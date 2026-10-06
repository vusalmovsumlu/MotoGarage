using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Profile;

public class UserProfileDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public UserRole Role { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }
}