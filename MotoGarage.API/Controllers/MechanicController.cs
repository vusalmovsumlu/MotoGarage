using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Mechanic;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/mechanics")]
[Authorize]
public class MechanicController : ControllerBase
{
    private readonly IMechanicService _mechanicService;

    public MechanicController(
        IMechanicService mechanicService)
    {
        _mechanicService = mechanicService;
    }

    // GET:
    // api/mechanics
    // api/mechanics?search=elektrik&address=Baku
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? address)
    {
        if (!string.IsNullOrWhiteSpace(search) ||
            !string.IsNullOrWhiteSpace(address))
        {
            var mechanics =
                await _mechanicService.SearchAsync(
                    search,
                    address);

            return Ok(mechanics);
        }

        var result =
            await _mechanicService.GetAllAsync();

        return Ok(result);
    }

    // GET: api/mechanics/eli-auto-service
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(
        string slug)
    {
        var mechanic =
            await _mechanicService.GetBySlugAsync(
                slug);

        return Ok(mechanic);
    }

    // GET: api/mechanics/my-profile
    [HttpGet("my-profile")]
    [Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId =
            GetCurrentUserId();

        var mechanic =
            await _mechanicService.GetMyProfileAsync(
                userId);

        return Ok(mechanic);
    }

    // POST: api/mechanics
    [HttpPost]
    [Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> Create(
        CreateMechanicDto dto)
    {
        var userId =
            GetCurrentUserId();

        var mechanic =
            await _mechanicService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetBySlug),
            new
            {
                slug = mechanic.Slug
            },
            mechanic);
    }

    // PUT: api/mechanics/my-profile
    [HttpPut("my-profile")]
    [Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> UpdateMyProfile(
        UpdateMechanicDto dto)
    {
        var userId =
            GetCurrentUserId();

        await _mechanicService.UpdateAsync(
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/mechanics/my-profile
    [HttpDelete("my-profile")]
    [Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> DeleteMyProfile()
    {
        var userId =
            GetCurrentUserId();

        await _mechanicService.DeleteAsync(
            userId);

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