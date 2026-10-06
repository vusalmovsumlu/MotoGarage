using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Admin;

public class AdminUserDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public UserRole Role { get; set; }

    public bool EmailConfirmed { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }
}