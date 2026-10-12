using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Range(1, 2147483647)]
    public int MechanicId { get; set; }

    [Range(1, 2147483647)]
    public int VehicleId { get; set; }

    public DateTime AppointmentDate { get; set; }
    [Range(15, 240)] public int DurationMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? Description { get; set; }
}