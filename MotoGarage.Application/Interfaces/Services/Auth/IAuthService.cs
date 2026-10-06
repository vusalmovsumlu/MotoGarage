using MotoGarage.Application.DTOs.Auth;

namespace MotoGarage.Application.Interfaces.Services.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);

    Task<AuthResponseDto> LoginAsync(LoginDto dto);

    Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto);

    Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto);

    Task LogoutAsync(string refreshToken);
}