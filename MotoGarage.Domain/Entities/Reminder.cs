namespace MotoGarage.Domain.Entities;

public class Reminder
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime ReminderDate { get; set; }

    public int? ReminderKm { get; set; }

    public bool IsCompleted { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}