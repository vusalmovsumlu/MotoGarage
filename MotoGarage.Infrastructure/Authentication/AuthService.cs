using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    // =========================
    // Register
    // =========================

    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser is not null)
        {
            throw new DomainException(
                "Bu email artıq qeydiyyatdan keçib.");
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
                dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                " ",
                result.Errors.Select(x => x.Description));

            throw new DomainException(errors);
        }

        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Login
    // =========================

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto)
    {
        var user =
            await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır.");
        }

        var passwordValid =
            await _userManager.CheckPasswordAsync(
                user,
                dto.Password);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır.");
        }

        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Google Login
    // =========================

    public async Task<AuthResponseDto> GoogleLoginAsync(
        GoogleLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IdToken))
        {
            throw new DomainException(
                "Google token göndərilməyib.");
        }

        var clientId =
            _configuration["Google:ClientId"];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Google ClientId konfiqurasiya edilməyib.");
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            var settings =
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[]
                    {
                        clientId
                    }
                };

            payload =
                await GoogleJsonWebSignature.ValidateAsync(
                    dto.IdToken,
                    settings);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException(
                "Google token etibarsızdır.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new DomainException(
                "Google hesabından email əldə edilə bilmədi.");
        }

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException(
                "Google email ünvanı təsdiqlənməyib.");
        }

        const string loginProvider = "Google";

        // Əvvəl Google hesabının artıq Identity-yə
        // qoşulub-qoşulmadığını yoxlayırıq
        var user =
            await _userManager.FindByLoginAsync(
                loginProvider,
                payload.Subject);

        if (user is not null)
        {
            return await CreateAuthResponseAsync(user);
        }

        // Google login tapılmadısa email üzrə user axtarırıq
        user =
            await _userManager.FindByEmailAsync(
                payload.Email);

        if (user is null)
        {
            // İlk dəfə Google ilə daxil olur
            user = new User
            {
                FullName =
                    !string.IsNullOrWhiteSpace(payload.Name)
                        ? payload.Name
                        : payload.Email,

                Email = payload.Email,
                UserName = payload.Email,

                EmailConfirmed = true,

                Role = UserRole.User,

                CreatedAt = DateTime.UtcNow
            };

            var createResult =
                await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    " ",
                    createResult.Errors
                        .Select(x => x.Description));

                throw new DomainException(errors);
            }
        }
        else
        {
            // User əvvəldən email/password ilə qeydiyyatdan keçibsə
            // Google verified email olduğuna görə email-i confirmed edirik
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;

                var updateResult =
                    await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(
                        " ",
                        updateResult.Errors
                            .Select(x => x.Description));

                    throw new DomainException(errors);
                }
            }
        }

        // Google hesabını həmin user-ə bağlayırıq
        var loginInfo =
            new UserLoginInfo(
                loginProvider,
                payload.Subject,
                "Google");

        var addLoginResult =
            await _userManager.AddLoginAsync(
                user,
                loginInfo);

        if (!addLoginResult.Succeeded)
        {
            var errors = string.Join(
                " ",
                addLoginResult.Errors
                    .Select(x => x.Description));

            throw new DomainException(errors);
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

        if (refreshToken is null ||
            !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException(
                "Refresh token etibarsızdır və ya vaxtı bitib.");
        }

        var user = refreshToken.User;

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "İstifadəçi tapılmadı.");
        }

        // Köhnə refresh token revoke olunur
        refreshToken.RevokedAt =
            DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(
            refreshToken);

        // Yeni refresh token yaradılır
        var newRefreshToken =
            new RefreshToken
            {
                Token =
                    _tokenService.CreateRefreshToken(),

                CreatedAt =
                    DateTime.UtcNow,

                ExpiresAt =
                    _tokenService.GetRefreshTokenExpiration(),

                UserId =
                    user.Id
            };

        await _refreshTokenRepository.AddAsync(
            newRefreshToken);

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
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
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var token =
            await _refreshTokenRepository
                .GetByTokenAsync(refreshToken);

        if (token is null)
        {
            return;
        }

        if (!token.IsActive)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(token);

        await _refreshTokenRepository.SaveChangesAsync();
    }

    // =========================
    // Create JWT + Refresh Token
    // =========================

    private async Task<AuthResponseDto>
        CreateAuthResponseAsync(User user)
    {
        var accessToken =
            _tokenService.CreateAccessToken(user);

        var refreshToken =
            new RefreshToken
            {
                Token =
                    _tokenService.CreateRefreshToken(),

                CreatedAt =
                    DateTime.UtcNow,

                ExpiresAt =
                    _tokenService.GetRefreshTokenExpiration(),

                UserId =
                    user.Id
            };

        await _refreshTokenRepository.AddAsync(
            refreshToken);

        await _refreshTokenRepository.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken =
                accessToken,

            RefreshToken =
                refreshToken.Token,

            AccessTokenExpiresAt =
                _tokenService.GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                refreshToken.ExpiresAt
        };
    }
}