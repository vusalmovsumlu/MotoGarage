using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Auth;

public class VerifyEmailDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = null!;

    [Required, RegularExpression(@"^[0-9]{6}$")]
    public string Code { get; set; } = null!;
}