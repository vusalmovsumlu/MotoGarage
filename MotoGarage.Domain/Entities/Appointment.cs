using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MechanicId { get; set; }

    public int VehicleId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string? Description { get; set; }

    public AppointmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }


    public User User { get; set; }

    public Mechanic Mechanic { get; set; }

    public Vehicle Vehicle { get; set; }
}