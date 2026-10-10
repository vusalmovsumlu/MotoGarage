namespace MotoGarage.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAppointmentReminderAsync(string email, MotoGarage.Application.DTOs.Appointment.AppointmentReminderEmail reminder, CancellationToken cancellationToken = default);
    Task SendVehicleHistoryAsync(string email, string vehicleName, byte[] attachment, string fileName, CancellationToken cancellationToken = default);
    Task SendVerificationCodeAsync(
        string email,
        string fullName,
        string code);
}
