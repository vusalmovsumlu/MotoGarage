using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Auth;

public class RegisterDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = null!;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required, StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = null!;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }
}