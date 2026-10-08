using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // GET: api/vehicle
    [HttpGet]
    public async Task<IActionResult> GetMyVehicles()
    {
        var userId = GetCurrentUserId();

        var vehicles =
            await _vehicleService.GetByUserIdAsync(userId);

        return Ok(vehicles);
    }

    // GET: api/vehicle/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var vehicles =
            await _vehicleService.GetByUserIdAsync(userId);

        var vehicle =
            vehicles.FirstOrDefault(x => x.Id == id);

        if (vehicle is null)
        {
            return NotFound(new
            {
                message = "Nəqliyyat vasitəsi tapılmadı."
            });
        }

        return Ok(vehicle);
    }

    // GET: api/vehicle/by-slug/toyota-corolla-...
    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug) =>
        Ok(await _vehicleService.GetBySlugAsync(slug, GetCurrentUserId()));

    // POST: api/vehicle
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateVehicleDto dto)
    {
        var userId = GetCurrentUserId();

        var vehicle =
            await _vehicleService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = vehicle.Id },
            vehicle);
    }

    // PUT: api/vehicle/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateVehicleDto dto)
    {
        var userId = GetCurrentUserId();

        await _vehicleService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/vehicle/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        await _vehicleService.DeleteAsync(
            id,
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
