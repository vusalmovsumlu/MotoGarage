using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Auth;

public class GoogleLoginDto
{
    [Required, StringLength(8192)]
    public string IdToken { get; set; } = null!;
}