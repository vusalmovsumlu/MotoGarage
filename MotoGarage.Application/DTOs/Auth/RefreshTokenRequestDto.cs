using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Auth;

public class RefreshTokenRequestDto
{
    [Required, StringLength(500)]
    public string RefreshToken { get; set; } = null!;
}