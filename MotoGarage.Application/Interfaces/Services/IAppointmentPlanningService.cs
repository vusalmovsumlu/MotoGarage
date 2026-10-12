using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Services;

public interface IAppointmentPlanningService
{
    Task<List<WorkingHourDto>> GetWorkingHoursAsync(int userId);
    Task SetWorkingHoursAsync(int userId, List<WorkingHourDto> hours);
    Task<AvailabilityDto> GetAvailabilityAsync(int mechanicId, DateOnly date, int durationMinutes);
    Task<List<AppointmentDto>> GetCalendarAsync(int userId, DateOnly from, DateOnly to);
    Task ValidateSlotAsync(int mechanicId, int vehicleId, DateTime date, int durationMinutes, int? excludeId = null);
    Task ProposeAsync(int id, int userId, RescheduleAppointmentDto dto);
    Task DecideRescheduleAsync(int id, int userId, AppointmentDecisionDto dto);
    Task<ServiceReportDto?> GetReportAsync(int id, int userId);
    Task SubmitReportAsync(int id, int userId, SubmitServiceReportDto dto);
    Task DecideReportAsync(int id, int userId, AppointmentDecisionDto dto);
}
