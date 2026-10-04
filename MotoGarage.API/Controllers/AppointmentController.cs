using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(
        IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // =========================
    // User appointments
    // =========================

    // GET: api/appointments/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = GetCurrentUserId();

        var appointments =
            await _appointmentService.GetMyAppointmentsAsync(
                userId);

        return Ok(appointments);
    }

    // =========================
    // Mechanic appointments
    // =========================

    // GET: api/appointments/mechanic
    [HttpGet("mechanic")]
    public async Task<IActionResult> GetMechanicAppointments()
    {
        var userId = GetCurrentUserId();

        var appointments =
            await _appointmentService
                .GetMechanicAppointmentsAsync(userId);

        return Ok(appointments);
    }

    // =========================
    // Appointment details
    // =========================

    // GET: api/appointments/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var appointment =
            await _appointmentService.GetByIdAsync(
                id,
                userId);

        return Ok(appointment);
    }

    // =========================
    // Create appointment
    // =========================

    // POST: api/appointments
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAppointmentDto dto)
    {
        var userId = GetCurrentUserId();

        var appointment =
            await _appointmentService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = appointment.Id },
            appointment);
    }

    // =========================
    // Mechanic changes status
    // =========================

    // PATCH: api/appointments/5/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateAppointmentStatusDto dto)
    {
        var userId = GetCurrentUserId();

        await _appointmentService.UpdateStatusAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // =========================
    // User cancellation
    // =========================

    // PATCH: api/appointments/5/cancel
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(
        int id)
    {
        var userId = GetCurrentUserId();

        await _appointmentService.CancelAsync(
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