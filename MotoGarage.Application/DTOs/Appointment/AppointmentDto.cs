using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Appointment;

public class AppointmentDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MechanicId { get; set; }

    public int VehicleId { get; set; }

    public string? VehicleName { get; set; }

    public string? MechanicName { get; set; }

    public string? CustomerName { get; set; }

    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? ProposedAppointmentDateUtc { get; set; }
    public int? RescheduleProposedByUserId { get; set; }
    public int RescheduleRevision { get; set; }

    public string? Description { get; set; }

    public AppointmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
