using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // =========================
    // Register
    // =========================

    // POST: api/auth/register
    [HttpPost("register")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterDto dto)
    {
        var result =
            await _authService.RegisterAsync(dto);

        return Ok(result);
    }

    [HttpPost("register-business")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-register")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<IActionResult> RegisterBusiness([FromForm] RegisterDto dto, [FromForm] List<IFormFile> photos)
    {
        if (dto.Role is not (MotoGarage.Domain.Enums.UserRole.Mechanic or MotoGarage.Domain.Enums.UserRole.Store))
            return BadRequest(new { message = "Bu qeydiyyat yalnız usta və mağaza üçündür." });
        if (photos.Count != 3) return BadRequest(new { message = "Dəqiq 3 şəkil seçin." });
        var streams = new List<RegistrationPhoto>();
        try
        {
            foreach (var photo in photos) streams.Add(new RegistrationPhoto(photo.OpenReadStream(), photo.FileName));
            return Ok(await _authService.RegisterAsync(dto, streams));
        }
        finally { foreach (var photo in streams) await photo.Stream.DisposeAsync(); }
    }

    // =========================
    // Verify Email OTP
    // =========================

    // POST: api/auth/verify-email
    [HttpPost("verify-email")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailDto dto)
    {
        var result =
            await _authService.VerifyEmailAsync(dto);

        return Ok(result);
    }

    // =========================
    // Resend OTP
    // =========================

    // POST: api/auth/resend-verification-code
    [HttpPost("resend-verification-code")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-resend")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerificationCode(
        ResendVerificationCodeDto dto)
    {
        await _authService
            .ResendVerificationCodeAsync(dto);

        return Ok(new
        {
            message =
                "Yeni təsdiqləmə kodu email ünvanınıza göndərildi."
        });
    }

    // =========================
    // Login
    // =========================

    // POST: api/auth/login
    [HttpPost("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginDto dto)
    {
        var result =
            await _authService.LoginAsync(dto);

        return Ok(result);
    }

    // =========================
    // Google Login
    // =========================

    // POST: api/auth/google
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin(
        GoogleLoginDto dto)
    {
        var result =
            await _authService.GoogleLoginAsync(dto);

        return Ok(result);
    }

    // =========================
    // Refresh Token
    // =========================

    // POST: api/auth/refresh-token
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken(
        RefreshTokenRequestDto dto)
    {
        var result =
            await _authService.RefreshTokenAsync(dto);

        return Ok(result);
    }

    // =========================
    // Logout
    // =========================

    // POST: api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        RefreshTokenRequestDto dto)
    {
        await _authService.LogoutAsync(
            dto.RefreshToken,
            int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value));

        return NoContent();
    }
}
