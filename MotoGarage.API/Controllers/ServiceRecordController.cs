using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.ServiceRecord;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/service-records")]
[Authorize]
public class ServiceRecordController : ControllerBase
{
    private readonly IServiceRecordService _serviceRecordService;

    public ServiceRecordController(
        IServiceRecordService serviceRecordService)
    {
        _serviceRecordService = serviceRecordService;
    }

    // GET: api/service-records/vehicle/5
    [HttpGet("vehicle/{vehicleId}")]
    public async Task<IActionResult> GetByVehicleId(
        int vehicleId)
    {
        var userId = GetCurrentUserId();

        var records =
            await _serviceRecordService.GetByVehicleIdAsync(
                vehicleId,
                userId);

        return Ok(records);
    }

    // GET: api/service-records/10
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var record =
            await _serviceRecordService.GetByIdAsync(
                id,
                userId);

        return Ok(record);
    }

    // POST: api/service-records
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateServiceRecordDto dto)
    {
        var userId = GetCurrentUserId();

        var record =
            await _serviceRecordService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = record.Id },
            record);
    }

    // PUT: api/service-records/10
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateServiceRecordDto dto)
    {
        var userId = GetCurrentUserId();

        await _serviceRecordService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/service-records/10
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        await _serviceRecordService.DeleteAsync(
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