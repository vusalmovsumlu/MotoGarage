using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Interfaces.Services.Auth;

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
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterDto dto)
    {
        var result =
            await _authService.RegisterAsync(dto);

        return Ok(result);
    }

    // =========================
    // Login
    // =========================

    // POST: api/auth/login
    [HttpPost("login")]
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
            dto.RefreshToken);

        return NoContent();
    }
}