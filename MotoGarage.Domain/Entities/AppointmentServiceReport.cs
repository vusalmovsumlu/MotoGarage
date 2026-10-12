namespace MotoGarage.Domain.Entities;

public class AppointmentServiceReport
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string ServiceName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ReplacedParts { get; set; }
    public int Kilometer { get; set; }
    public decimal Cost { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Status { get; set; } = "Pending";
    public int Revision { get; set; } = 1;
    public string? DecisionComment { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Appointment Appointment { get; set; } = null!;
}
