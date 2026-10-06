using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Profile;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _profileService;

    public ProfileController(
        IUserProfileService profileService)
    {
        _profileService = profileService;
    }

    // GET: api/profile
    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();

        var profile =
            await _profileService.GetMyProfileAsync(
                userId);

        return Ok(profile);
    }

    // PUT: api/profile
    [HttpPut]
    public async Task<IActionResult> Update(
        UpdateUserProfileDto dto)
    {
        var userId = GetCurrentUserId();

        await _profileService.UpdateAsync(
            userId,
            dto);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "İstifadəçi məlumatı tapılmadı.");
        }

        return id;
    }
}