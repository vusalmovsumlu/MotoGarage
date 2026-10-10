using MotoGarage.Application.Helpers;
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
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly Microsoft.Extensions.Logging.ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationCodeRepository _emailVerificationCodeRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IImageService _images;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationCodeRepository emailVerificationCodeRepository,
        IEmailService emailService,
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        Microsoft.Extensions.Logging.ILogger<AuthService> logger,
        AppDbContext context, IImageService images)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _emailVerificationCodeRepository = emailVerificationCodeRepository;
        _emailService = emailService;
        _configuration = configuration;
        _context = context;
        _images = images;
    }

    // =========================
    // Register
    // =========================

    public async Task<RegisterResponseDto> RegisterAsync(
        RegisterDto dto, IReadOnlyList<RegistrationPhoto>? photos = null)
    {
        DtoValidation.Validate(dto);
        var business = dto.Role is UserRole.Mechanic or UserRole.Store;
        if (business)
            await ValidateRegistrationPhotosAsync(photos);
        else if (photos?.Count > 0)
            throw new DomainException("Şəkilli qeydiyyat yalnız usta və mağaza üçündür.");
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

        dto.Email = dto.Email.Trim();
        dto.FullName = dto.FullName.Trim();
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
            BusinessApprovalStatus = business ? BusinessApprovalStatus.Pending : BusinessApprovalStatus.Approved,
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

        if (business)
        {
            var uploaded = new List<MotoGarage.Application.DTOs.Media.UploadResultDto>();
            try
            {
                foreach (var photo in photos!)
                    uploaded.Add(await _images.UploadAsync(photo.Stream, photo.FileName, user.Id));
                _context.BusinessApplications.Add(new BusinessApplication
                {
                    UserId = user.Id, CreatedAt = DateTime.UtcNow,
                    Photos = uploaded.Select((image, index) => new BusinessApplicationPhoto
                    { Position = index + 1, PublicId = image.PublicId, Url = image.Url }).ToList()
                });
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Remote uploads are outside SQL retry delegates. Compensate a partially created registration.
                _context.ChangeTracker.Clear();
                try
                {
                    await _unitOfWork.ExecuteAsync(async () =>
                    {
                        _context.BusinessApplications.RemoveRange(await _context.BusinessApplications.Where(x => x.UserId == user.Id).ToListAsync());
                        _context.ImageAssets.RemoveRange(await _context.ImageAssets.Where(x => x.UserId == user.Id).ToListAsync());
                        await _context.SaveChangesAsync();
                        var registered = await _userManager.FindByIdAsync(user.Id.ToString());
                        if (registered is not null) ThrowIdentityErrorsIfFailed(await _userManager.DeleteAsync(registered));
                    });
                }
                catch (Exception failure) { _logger.LogError(failure, "Failed registration cleanup for {UserId}", user.Id); }
                foreach (var image in uploaded)
                {
                    try { await _images.DeleteAsync(image.PublicId); }
                    catch (Exception failure) { _logger.LogWarning(failure, "Failed registration image cleanup"); }
                }
                throw;
            }
        }

        var sent = await CreateAndSendVerificationCodeAsync(user);

        return new RegisterResponseDto
        {
            Email = user.Email!,
            Message =
                sent ? "Qeydiyyat başladı. Email ünvanınıza təsdiqləmə kodu göndərildi."
                    : "Hesab yaradıldı, email göndərilmədi. Resend endpoint ilə təsdiqləməni davam etdir.",
            ExpiresInMinutes = 10
        };
    }

    // =========================
    // Verify Email
    // =========================

    public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
    {
        DtoValidation.Validate(dto);
        var result = await _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
            ThrowIfBlocked(user);
            if (user.EmailConfirmed) throw new DomainException("Email artıq təsdiqlənib.");
            var code = await _emailVerificationCodeRepository.GetLatestByUserIdAsync(user.Id);
            if (code is null || code.UsedAt is not null || code.ExpiresAt <= DateTime.UtcNow)
                throw new DomainException("Təsdiqləmə kodu etibarsızdır və ya vaxtı bitib.");
            if (code.FailedAttempts >= 5)
                throw new DomainException("Maksimum səhv cəhd sayı keçilib. Yeni kod tələb et.");
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(HashVerificationCode(dto.Code)),
                    Convert.FromHexString(code.CodeHash)))
            {
                code.FailedAttempts++;
                await _emailVerificationCodeRepository.SaveChangesAsync();
                return (Response: (AuthResponseDto?)null, Error: "Təsdiqləmə kodu yanlışdır.");
            }
            code.UsedAt = DateTime.UtcNow;
            user.EmailConfirmed = true;
            var updated = await _userManager.UpdateAsync(user);
            if (!updated.Succeeded) ThrowIdentityErrors(updated);
            var response = await CreateAuthResponseAsync(user);
            return (Response: (AuthResponseDto?)response, Error: (string?)null);
        });
        if (result.Error is not null) throw new DomainException(result.Error);
        return result.Response!;
    }

    // =========================
    // Resend Verification Code
    // =========================

    public async Task ResendVerificationCodeAsync(ResendVerificationCodeDto dto)
    {
        DtoValidation.Validate(dto);
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
        ThrowIfBlocked(user);
        if (user.EmailConfirmed) throw new DomainException("Email artıq təsdiqlənib.");
        if (!await CreateAndSendVerificationCodeAsync(user))
            throw new DomainException("Email göndərilmədi. Resend ilə yenidən cəhd et.");
    }

    // =========================
    // Login
    // =========================

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto)
    {
        DtoValidation.Validate(dto);
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
            var failedResult = await _userManager.AccessFailedAsync(user);
            if (!failedResult.Succeeded) ThrowIdentityErrors(failedResult);
            throw new UnauthorizedAccessException(
                "Email və ya şifrə yanlışdır.");
        }

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAccessException(
                "Əvvəlcə email ünvanını təsdiqləməlisən.");
        }

        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetResult.Succeeded) ThrowIdentityErrors(resetResult);
        return await CreateAuthResponseAsync(user);
    }

    // =========================
    // Google Login
    // =========================

    public async Task<AuthResponseDto> GoogleLoginAsync(
        GoogleLoginDto dto)
    {
        DtoValidation.Validate(dto);
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
                throw new UnauthorizedAccessException(
                    "Əvvəlcə lokal hesabın email təsdiqləmə prosesini tamamlamalısan.");
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

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        DtoValidation.Validate(dto);
        var response = await _unitOfWork.ExecuteAsync<AuthResponseDto?>(async () =>
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken);
            if (token is null) return null;
            if (token.IsRevoked)
            {
                // Reuse invalidates all sessions for this user, including the rotated token.
                await RevokeUserTokensAsync(token.UserId);
                return null;
            }
            if (!token.IsActive) return null;
            ThrowIfBlocked(token.User);
            if (!token.User.EmailConfirmed)
                throw new UnauthorizedAccessException("Email təsdiqlənməyib.");
            token.RevokedAt = DateTime.UtcNow;
            return await CreateAuthResponseAsync(token.User);
        });
        return response ?? throw new UnauthorizedAccessException(
            "Refresh token etibarsızdır və ya təkrar istifadə olunub.");
    }

    private async Task RevokeUserTokensAsync(int userId)
    {
        var tokens = await _refreshTokenRepository.FindAsync(x =>
            x.UserId == userId && x.RevokedAt == null);
        foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();
    }

    // =========================
    // Logout
    // =========================

    public async Task LogoutAsync(string refreshToken, int userId)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token is null) return;
            if (token.UserId != userId)
                throw new ForbiddenException("Bu sessiya sənə aid deyil.");
            if (!token.IsActive) return;
            token.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync();
        });
    }

    // =========================
    // OTP Helper
    // =========================

    private async Task<bool> CreateAndSendVerificationCodeAsync(User user)
    {
        var prepared = await _unitOfWork.ExecuteAsync(async () =>
        {
            var current = await _userManager.FindByIdAsync(user.Id.ToString())
                ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
            ThrowIfBlocked(current);
            if (current.EmailConfirmed) throw new DomainException("Email artıq təsdiqlənib.");
            var latest = await _emailVerificationCodeRepository.GetLatestByUserIdAsync(current.Id);
            if (latest is not null && latest.UsedAt is null && latest.CreatedAt.AddMinutes(1) > DateTime.UtcNow)
                throw new DomainException("Yeni kod üçün ən azı 1 dəqiqə gözləməlisən.");
            var unused = await _emailVerificationCodeRepository.GetUnusedByUserIdAsync(current.Id);
            foreach (var previous in unused) previous.UsedAt = DateTime.UtcNow;
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var record = new EmailVerificationCode
            {
                UserId = current.Id,
                CodeHash = HashVerificationCode(code),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            await _emailVerificationCodeRepository.AddAsync(record);
            await _emailVerificationCodeRepository.SaveChangesAsync();
            return (record.Id, current.Email, current.FullName, Code: code);
        });
        try
        {
            // Delivery runs after commit and is never repeated by a SQL deadlock retry.
            await _emailService.SendVerificationCodeAsync(prepared.Email!, prepared.FullName, prepared.Code);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Verification email delivery failed for user {UserId}: {ErrorType}",
                user.Id, ex.GetType().Name);
            await _unitOfWork.ExecuteAsync(async () =>
            {
                var record = await _emailVerificationCodeRepository.GetByIdAsync(prepared.Id);
                if (record is not null && record.UsedAt is null) record.UsedAt = DateTime.UtcNow;
                await _emailVerificationCodeRepository.SaveChangesAsync();
            });
            return false;
        }
    }

    private static void ThrowIdentityErrorsIfFailed(IdentityResult result)
    {
        if (!result.Succeeded) ThrowIdentityErrors(result);
    }

    private static async Task ValidateRegistrationPhotosAsync(IReadOnlyList<RegistrationPhoto>? photos)
    {
        if (photos?.Count != 3) throw new DomainException("Usta və mağaza qeydiyyatı üçün dəqiq 3 şəkil məcburidir.");
        var hashes = new HashSet<string>();
        foreach (var photo in photos)
        {
            var stream = photo.Stream;
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) || !stream.CanSeek || stream.Length == 0 || stream.Length > 5 * 1024 * 1024)
                throw new DomainException("Hər şəkil JPG, PNG və ya WEBP formatında, maksimum 5 MB olmalıdır.");
            stream.Position = 0;
            var header = new byte[12];
            var length = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            var valid = extension is ".jpg" or ".jpeg" ? length >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff
                : extension == ".png" ? length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10})
                : length >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            if (!valid) throw new DomainException("Şəklin fayl formatı uyğun deyil.");
            stream.Position = 0;
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
            stream.Position = 0;
            if (!hashes.Add(hash)) throw new DomainException("3 fərqli şəkil seçin; eyni şəkil təkrar qəbul edilmir.");
        }
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
        var isBlocked = user.DeletedAtUtc != null || user.IsAdminBlocked ||
            (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

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

        if (!user.EmailConfirmed)
            throw new UnauthorizedAccessException("Email təsdiqlənməyib.");
        var rawToken = _tokenService.CreateRefreshToken();
        var refreshToken =
            new RefreshToken
            {
                Token = TokenHash.Compute(rawToken),

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
                rawToken,

            AccessTokenExpiresAt =
                _tokenService
                    .GetAccessTokenExpiration(),

            RefreshTokenExpiresAt =
                refreshToken.ExpiresAt
        };
    }
}
