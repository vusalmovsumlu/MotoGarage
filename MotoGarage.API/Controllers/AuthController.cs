using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.Interfaces.Services.Auth;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        return Ok(result);
    }

    // =========================
    // Login
    // =========================

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        return Ok(result);
    }

    // =========================
    // Refresh Token
    // =========================

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(
        RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto);

        return Ok(result);
    }

    // =========================
    // Logout
    // =========================

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshTokenRequestDto dto)
    {
        await _authService.LogoutAsync(dto.RefreshToken);

        return NoContent();
    }
}