using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

namespace MotoGarage.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationCodeRepository _emailVerificationCodeRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationCodeRepository emailVerificationCodeRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _emailVerificationCodeRepository = emailVerificationCodeRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    // =========================
    // Register
    // =========================

    public async Task<RegisterResponseDto> RegisterAsync(
        RegisterDto dto)
    {
        if (dto.Role == UserRole.Admin)
        {
            throw new DomainException(
                "Admin hesabı qeydiyyat vasitəsilə yaradıla bilməz.");
        }

        if (dto.Role != UserRole.User &&
            dto.Role != UserRole.Mechanic &&
            dto.Role != UserRole.Store)
        {
            throw new DomainException(
                "İstifadəçi rolu düzgün deyil.");
        }

        var existingUser =
            await _userManager.FindByEmailAsync(dto.Email);

        if (existingUser is not null)
        {
            if (!existingUser.EmailConfirmed)
            {
                throw new DomainException(
                    "Bu email artıq qeydiyyatdan keçib, amma təsdiqlənməyib. Yeni kod göndərə bilərsən.");
            }

            throw new DomainException(
                "Bu email artıq qeydiyyatdan keçib.");
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            UserName = dto.Email,
            EmailConfirmed = false,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow
        };

        var result =
            await _userManager.CreateAsync(
                user,
                dto.Password);

        if (!result.Succeeded)
        {
            ThrowIdentityErrors(result);
        }

        await CreateAndSendVerificationCodeAsync(user);

        return new RegisterResponseDto
        {
            Email = user.Email!,
            Message =
                "Qeydiyyat başladı. Email ünvanınıza təsdiqləmə kodu göndərildi.",
            ExpiresInMinutes = 10
        };
    }

    // =========================
    // Verify Email
    // =========================

    public async Task<AuthResponseDto> VerifyEmailAsync(
        VerifyEmailDto dto)
    {
        var user =
            await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");
        }

        ThrowIfBlocked(user);

        if (user.EmailConfirmed)
        {
            throw new DomainException(
                "Email artıq təsdiqlənib.");
        }

        var verificationCode =
            await _emailVerificationCodeRepository
                .GetLatestByUserIdAsync(user.Id);

        if (verificationCode is null)
        {
            throw new DomainException(
                "Təsdiqləmə kodu tapılmadı.");
        }

        if (verificationCode.UsedAt is not null)
        {
            throw new DomainException(
                "Bu təsdiqləmə kodu artıq istifadə olunub.");
        }

        if (verificationCode.ExpiresAt <= DateTime.UtcNow)
        {
            throw new DomainException(
                "Təsdiqləmə kodunun vaxtı bitib.");
        }

        if (verificationCode.FailedAttempts >= 5)
        {
            throw new DomainException(
                "Maksimum səhv cəhd sayı keçilib. Yeni kod tələb et.");
        }

        var incomingHash =
            HashVerificationCode(dto.Code);

        if (!string.Equals(
                incomingHash,
                verificationCode.CodeHash,
                StringComparison.Ordinal))
        {
            verificationCode.FailedAttempts++;

            await _emailVerificationCodeRepository
                .UpdateAsync(verificationCode);

            await _emailVerificationCodeRepository
                .SaveChangesAsync();

            throw new DomainException(
                "Təsdiqləmə kodu yanlışdır.");
        }

        verificationCode.UsedAt =
            DateTime.UtcNow;

        await _emailVerificationCodeRepository
            .UpdateAsync(verificationCode);

        user.EmailConfirmed = true;

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            ThrowIdentityErrors(updateResult);
        }

        await _emailVerificationCodeRepository
            .SaveChangesAsync();

        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Resend Verification Code
    // =========================

    public async Task ResendVerificationCodeAsync(
        ResendVerificationCodeDto dto)
    {
        var user =
            await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");
        }

        ThrowIfBlocked(user);

        if (user.EmailConfirmed)
        {
            throw new DomainException(
                "Email artıq təsdiqlənib.");
        }

        var latestCode =
            await _emailVerificationCodeRepository
                .GetLatestByUserIdAsync(user.Id);

        if (latestCode is not null &&
            latestCode.CreatedAt.AddMinutes(1) >
            DateTime.UtcNow)
        {
            throw new DomainException(
                "Yeni kod üçün ən azı 1 dəqiqə gözləməlisən.");
        }

        var unusedCodes =
            await _emailVerificationCodeRepository
                .GetUnusedByUserIdAsync(user.Id);

        foreach (var code in unusedCodes)
        {
            code.UsedAt = DateTime.UtcNow;

            await _emailVerificationCodeRepository
                .UpdateAsync(code);
        }

        await _emailVerificationCodeRepository
            .SaveChangesAsync();

        await CreateAndSendVerificationCodeAsync(user);
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

        ThrowIfBlocked(user);

        var passwordValid =
            await _userManager.CheckPasswordAsync(
                user,
                dto.Password);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır.");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAccessException(
                "Əvvəlcə email ünvanını təsdiqləməlisən.");
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

        var user =
            await _userManager.FindByLoginAsync(
                loginProvider,
                payload.Subject);

        if (user is not null)
        {
            ThrowIfBlocked(user);

            return await CreateAuthResponseAsync(user);
        }

        user =
            await _userManager.FindByEmailAsync(
                payload.Email);

        if (user is null)
        {
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
                ThrowIdentityErrors(createResult);
            }
        }
        else
        {
            ThrowIfBlocked(user);

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;

                var updateResult =
                    await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    ThrowIdentityErrors(updateResult);
                }
            }
        }

        var logins =
            await _userManager.GetLoginsAsync(user);

        var alreadyLinked =
            logins.Any(x =>
                x.LoginProvider == loginProvider &&
                x.ProviderKey == payload.Subject);

        if (!alreadyLinked)
        {
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
                ThrowIdentityErrors(addLoginResult);
            }
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

        var user =
            refreshToken.User;

        if (user is null)
        {
            throw new UnauthorizedAccessException(
                "İstifadəçi tapılmadı.");
        }

        ThrowIfBlocked(user);

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAccessException(
                "Email təsdiqlənməyib.");
        }

        refreshToken.RevokedAt =
            DateTime.UtcNow;

        await _refreshTokenRepository
            .UpdateAsync(refreshToken);

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

        await _refreshTokenRepository
            .AddAsync(newRefreshToken);

        await _refreshTokenRepository
            .SaveChangesAsync();

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

    public async Task LogoutAsync(
        string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var token =
            await _refreshTokenRepository
                .GetByTokenAsync(refreshToken);

        if (token is null ||
            !token.IsActive)
        {
            return;
        }

        token.RevokedAt =
            DateTime.UtcNow;

        await _refreshTokenRepository
            .UpdateAsync(token);

        await _refreshTokenRepository
            .SaveChangesAsync();
    }

    // =========================
    // OTP Helper
    // =========================

    private async Task CreateAndSendVerificationCodeAsync(
        User user)
    {
        var code =
            RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

        var verificationCode =
            new EmailVerificationCode
            {
                UserId = user.Id,
                CodeHash =
                    HashVerificationCode(code),

                CreatedAt =
                    DateTime.UtcNow,

                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(10),

                FailedAttempts = 0
            };

        await _emailVerificationCodeRepository
            .AddAsync(verificationCode);

        await _emailVerificationCodeRepository
            .SaveChangesAsync();

        await _emailService
            .SendVerificationCodeAsync(
                user.Email!,
                user.FullName,
                code);
    }

    private static string HashVerificationCode(
        string code)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(code));

        return Convert.ToHexString(bytes);
    }

    // =========================
    // Block Check
    // =========================

    private static void ThrowIfBlocked(
        User user)
    {
        var isBlocked =
            user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value >
            DateTimeOffset.UtcNow;

        if (isBlocked)
        {
            throw new UnauthorizedAccessException(
                "Bu hesab administrator tərəfindən bloklanıb.");
        }
    }

    // =========================
    // Identity Errors
    // =========================

    private static void ThrowIdentityErrors(
        IdentityResult result)
    {
        var errors =
            string.Join(
                " ",
                result.Errors.Select(
                    x => x.Description));

        throw new DomainException(errors);
    }

    // =========================
    // JWT + Refresh Token
    // =========================

    private async Task<AuthResponseDto>
        CreateAuthResponseAsync(User user)
    {
        ThrowIfBlocked(user);

        var refreshToken =
            new RefreshToken
            {
                Token =
                    _tokenService.CreateRefreshToken(),

                CreatedAt =
                    DateTime.UtcNow,

                ExpiresAt =
                    _tokenService
                        .GetRefreshTokenExpiration(),

                UserId =
                    user.Id
            };

        await _refreshTokenRepository
            .AddAsync(refreshToken);

        await _refreshTokenRepository
            .SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken =
                _tokenService.CreateAccessToken(user),

            RefreshToken =
                refreshToken.Token,

            AccessTokenExpiresAt =
                _tokenService
                    .GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                refreshToken.ExpiresAt
        };
    }
}