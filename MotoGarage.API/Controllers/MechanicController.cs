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
    //
    // GET:
    // api/mechanics?search=elektrik
    //
    // GET:
    // api/mechanics?search=elektrik&address=Baku
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? address)
    {
        if (!string.IsNullOrWhiteSpace(search) ||
            !string.IsNullOrWhiteSpace(address))
        {
            var filteredMechanics =
                await _mechanicService.SearchAsync(
                    search,
                    address);

            return Ok(filteredMechanics);
        }

        var mechanics =
            await _mechanicService.GetAllAsync();

        return Ok(mechanics);
    }

    // GET: api/mechanics/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var mechanic =
            await _mechanicService.GetByIdAsync(id);

        return Ok(mechanic);
    }

    // GET: api/mechanics/my-profile
    [HttpGet("my-profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();

        var mechanic =
            await _mechanicService.GetMyProfileAsync(
                userId);

        return Ok(mechanic);
    }

    // POST: api/mechanics
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateMechanicDto dto)
    {
        var userId = GetCurrentUserId();

        var mechanic =
            await _mechanicService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = mechanic.Id },
            mechanic);
    }

    // PUT: api/mechanics/my-profile
    [HttpPut("my-profile")]
    public async Task<IActionResult> UpdateMyProfile(
        UpdateMechanicDto dto)
    {
        var userId = GetCurrentUserId();

        await _mechanicService.UpdateAsync(
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/mechanics/my-profile
    [HttpDelete("my-profile")]
    public async Task<IActionResult> DeleteMyProfile()
    {
        var userId = GetCurrentUserId();

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