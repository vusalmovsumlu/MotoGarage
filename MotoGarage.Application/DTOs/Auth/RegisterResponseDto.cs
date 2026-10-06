namespace MotoGarage.Application.DTOs.Auth;

public class RegisterResponseDto
{
    public string Email { get; set; } = null!;

    public string Message { get; set; } = null!;

    public int ExpiresInMinutes { get; set; }
}