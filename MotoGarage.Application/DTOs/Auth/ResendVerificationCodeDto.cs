using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Auth;

public class ResendVerificationCodeDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;
}