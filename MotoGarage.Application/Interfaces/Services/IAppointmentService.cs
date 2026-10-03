using MotoGarage.Application.DTOs.Appointment;

namespace MotoGarage.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentDto>> GetMyAppointmentsAsync(
        int userId);

    Task<IEnumerable<AppointmentDto>> GetMechanicAppointmentsAsync(
        int mechanicUserId);

    Task<AppointmentDto> GetByIdAsync(
        int id,
        int currentUserId);

    Task<AppointmentDto> CreateAsync(
        int userId,
        CreateAppointmentDto dto);

    Task UpdateStatusAsync(
        int appointmentId,
        int mechanicUserId,
        UpdateAppointmentStatusDto dto);

    Task CancelAsync(
        int appointmentId,
        int userId);
}