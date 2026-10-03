using Microsoft.AspNetCore.Identity;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    // =========================
    // Register
    // =========================

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser is not null)
        {
            throw new DomainException(
                "Bu email artıq istifadə olunur."
            );
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow
        };

        var result =
            await _userManager.CreateAsync(
                user,
                dto.Password
            );

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(x => x.Description)
            );

            throw new DomainException(errors);
        }

        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Login
    // =========================

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user =
            await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır."
            );
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(
                user,
                dto.Password
            );

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır."
            );
        }

        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Refresh Token
    // =========================

    public async Task<AuthResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto dto)
    {
        var refreshToken =
            await _refreshTokenRepository
                .GetByTokenAsync(dto.RefreshToken);

        if (refreshToken is null)
        {
            throw new UnauthorizedAccessException(
                "Refresh token etibarsızdır."
            );
        }

        if (!refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Refresh token etibarsızdır və ya vaxtı bitib."
            );
        }

        var user = refreshToken.User;

        // Köhnə refresh token bağlanır
        refreshToken.RevokedAt = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(
            refreshToken
        );

        // Yeni refresh token yaradılır
        var newRefreshToken = new RefreshToken
        {
            Token = _tokenService.CreateRefreshToken(),

            UserId = user.Id,

            CreatedAt = DateTime.UtcNow,

            ExpiresAt =
                _tokenService.GetRefreshTokenExpiration()
        };

        await _refreshTokenRepository.AddAsync(
            newRefreshToken
        );

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,

            FullName = user.FullName,

            Email = user.Email!,

            Role = user.Role.ToString(),

            AccessToken =
                _tokenService.CreateAccessToken(user),

            RefreshToken =
                newRefreshToken.Token,

            AccessTokenExpiresAt =
                _tokenService.GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                newRefreshToken.ExpiresAt
        };
    }

    // =========================
    // Logout
    // =========================

    public async Task LogoutAsync(string refreshToken)
    {
        var token =
            await _refreshTokenRepository
                .GetByTokenAsync(refreshToken);

        if (token is null)
            return;

        if (!token.IsActive)
            return;

        token.RevokedAt = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(token);

        await _refreshTokenRepository.SaveChangesAsync();
    }

    // =========================
    // Create Auth Response
    // =========================

    private async Task<AuthResponseDto> CreateAuthResponseAsync(
        User user)
    {
        var refreshToken = new RefreshToken
        {
            Token =
                _tokenService.CreateRefreshToken(),

            UserId = user.Id,

            CreatedAt = DateTime.UtcNow,

            ExpiresAt =
                _tokenService.GetRefreshTokenExpiration()
        };

        await _refreshTokenRepository.AddAsync(
            refreshToken
        );

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
            UserId = user.Id,

            FullName = user.FullName,

            Email = user.Email!,

            Role = user.Role.ToString(),

            AccessToken =
                _tokenService.CreateAccessToken(user),

            RefreshToken =
                refreshToken.Token,

            AccessTokenExpiresAt =
                _tokenService.GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                refreshToken.ExpiresAt
        };
    }
}