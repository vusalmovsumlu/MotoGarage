namespace MotoGarage.Application.DTOs.Auth;

public class VerifyEmailDto
{
    public string Email { get; set; } = null!;

    public string Code { get; set; } = null!;
}