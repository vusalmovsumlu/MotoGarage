using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Appointment;

public record AppointmentReminderEmail(
    int AppointmentId,
    string CustomerName,
    DateTime AppointmentDateUtc,
    string VehicleName,
    string MechanicName,
    string Address,
    AppointmentStatus Status);
