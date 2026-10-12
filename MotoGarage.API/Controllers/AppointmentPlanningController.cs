using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Roles = "User,Mechanic,Store")]
public class AppointmentPlanningController(IAppointmentPlanningService planning) : ControllerBase
{
    int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("working-hours"), Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> Hours() => Ok(await planning.GetWorkingHoursAsync(UserId));
    [HttpPut("working-hours"), Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> SaveHours(List<WorkingHourDto> hours) { await planning.SetWorkingHoursAsync(UserId, hours); return NoContent(); }
    [HttpGet("availability")]
    public async Task<IActionResult> Availability(int mechanicId, DateOnly date, int durationMinutes = 60) =>
        Ok(await planning.GetAvailabilityAsync(mechanicId, date, durationMinutes));
    [HttpGet("calendar"), Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> Calendar(DateOnly from, DateOnly to) => Ok(await planning.GetCalendarAsync(UserId, from, to));
    [HttpPost("{id:int:min(1)}/reschedule")]
    public async Task<IActionResult> Propose(int id, RescheduleAppointmentDto dto) { await planning.ProposeAsync(id, UserId, dto); return NoContent(); }
    [HttpPost("{id:int:min(1)}/reschedule/decision")]
    public async Task<IActionResult> Decide(int id, AppointmentDecisionDto dto) { await planning.DecideRescheduleAsync(id, UserId, dto); return NoContent(); }
    [HttpGet("{id:int:min(1)}/report")]
    public async Task<IActionResult> Report(int id) => Ok(await planning.GetReportAsync(id, UserId));
    [HttpPut("{id:int:min(1)}/report"), Authorize(Roles = "Mechanic")]
    public async Task<IActionResult> SubmitReport(int id, SubmitServiceReportDto dto) { await planning.SubmitReportAsync(id, UserId, dto); return NoContent(); }
    [HttpPost("{id:int:min(1)}/report/decision")]
    public async Task<IActionResult> DecideReport(int id, AppointmentDecisionDto dto) { await planning.DecideReportAsync(id, UserId, dto); return NoContent(); }
}
