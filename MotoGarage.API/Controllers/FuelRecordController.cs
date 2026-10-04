using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/fuel-records")]
[Authorize]
public class FuelRecordController : ControllerBase
{
    private readonly IFuelRecordService _fuelRecordService;

    public FuelRecordController(
        IFuelRecordService fuelRecordService)
    {
        _fuelRecordService = fuelRecordService;
    }

    // GET: api/fuel-records/vehicle/5
    [HttpGet("vehicle/{vehicleId}")]
    public async Task<IActionResult> GetByVehicleId(
        int vehicleId)
    {
        var userId = GetCurrentUserId();

        var records =
            await _fuelRecordService.GetByVehicleIdAsync(
                vehicleId,
                userId);

        return Ok(records);
    }

    // GET: api/fuel-records/10
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var record =
            await _fuelRecordService.GetByIdAsync(
                id,
                userId);

        return Ok(record);
    }

    // POST: api/fuel-records
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateFuelRecordDto dto)
    {
        var userId = GetCurrentUserId();

        var record =
            await _fuelRecordService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = record.Id },
            record);
    }

    // PUT: api/fuel-records/10
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateFuelRecordDto dto)
    {
        var userId = GetCurrentUserId();

        await _fuelRecordService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/fuel-records/10
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        await _fuelRecordService.DeleteAsync(
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