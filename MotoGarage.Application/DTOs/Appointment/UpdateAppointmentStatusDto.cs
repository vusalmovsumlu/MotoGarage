using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Appointment;

public class UpdateAppointmentStatusDto
{
    public AppointmentStatus Status { get; set; }
}