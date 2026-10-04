using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
public class ReminderController : ControllerBase
{
    private readonly IReminderService _reminderService;

    public ReminderController(
        IReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    // GET: api/reminders/vehicle/5
    [HttpGet("vehicle/{vehicleId}")]
    public async Task<IActionResult> GetByVehicleId(
        int vehicleId)
    {
        var userId = GetCurrentUserId();

        var reminders =
            await _reminderService.GetByVehicleIdAsync(
                vehicleId,
                userId);

        return Ok(reminders);
    }

    // GET: api/reminders/vehicle/5/active
    [HttpGet("vehicle/{vehicleId}/active")]
    public async Task<IActionResult> GetActive(
        int vehicleId)
    {
        var userId = GetCurrentUserId();

        var reminders =
            await _reminderService.GetActiveAsync(
                vehicleId,
                userId);

        return Ok(reminders);
    }

    // GET: api/reminders/vehicle/5/due
    [HttpGet("vehicle/{vehicleId}/due")]
    public async Task<IActionResult> GetDue(
        int vehicleId)
    {
        var userId = GetCurrentUserId();

        var reminders =
            await _reminderService.GetDueAsync(
                vehicleId,
                userId);

        return Ok(reminders);
    }

    // GET: api/reminders/10
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var reminder =
            await _reminderService.GetByIdAsync(
                id,
                userId);

        return Ok(reminder);
    }

    // POST: api/reminders
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateReminderDto dto)
    {
        var userId = GetCurrentUserId();

        var reminder =
            await _reminderService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = reminder.Id },
            reminder);
    }

    // PUT: api/reminders/10
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateReminderDto dto)
    {
        var userId = GetCurrentUserId();

        await _reminderService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // PATCH: api/reminders/10/complete
    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> MarkAsCompleted(
        int id)
    {
        var userId = GetCurrentUserId();

        await _reminderService.MarkAsCompletedAsync(
            id,
            userId);

        return NoContent();
    }

    // DELETE: api/reminders/10
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        var userId = GetCurrentUserId();

        await _reminderService.DeleteAsync(
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