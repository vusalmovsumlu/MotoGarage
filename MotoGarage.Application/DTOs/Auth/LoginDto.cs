using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Auth;

public class LoginDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required, StringLength(128)]
    public string Password { get; set; } = null!;
}