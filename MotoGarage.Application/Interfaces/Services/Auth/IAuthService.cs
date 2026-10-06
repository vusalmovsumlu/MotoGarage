using MotoGarage.Application.DTOs.Auth;

namespace MotoGarage.Application.Interfaces.Services;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(
        RegisterDto dto);

    Task<AuthResponseDto> VerifyEmailAsync(
        VerifyEmailDto dto);

    Task ResendVerificationCodeAsync(
        ResendVerificationCodeDto dto);

    Task<AuthResponseDto> LoginAsync(
        LoginDto dto);

    Task<AuthResponseDto> GoogleLoginAsync(
        GoogleLoginDto dto);

    Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto);

    Task LogoutAsync(string refreshToken);
}