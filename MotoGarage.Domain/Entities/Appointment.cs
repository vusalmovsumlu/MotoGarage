using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Appointment
{
    public int Id { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int UserId { get; set; }

    public int MechanicId { get; set; }

    public int VehicleId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string? Description { get; set; }

    public AppointmentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EmailReminderSentAtUtc { get; set; }

    public Guid? EmailReminderLeaseId { get; set; }

    public DateTime? EmailReminderLeaseUntilUtc { get; set; }

    public DateTime? EmailReminderNextAttemptAtUtc { get; set; }


    public User User { get; set; } = null!;

    public Mechanic Mechanic { get; set; } = null!;

    public Vehicle Vehicle { get; set; } = null!;
}
