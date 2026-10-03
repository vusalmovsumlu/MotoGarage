namespace MotoGarage.Application.DTOs.Appointment;

public class CreateAppointmentDto
{
    public int MechanicId { get; set; }

    public int VehicleId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string? Description { get; set; }
}