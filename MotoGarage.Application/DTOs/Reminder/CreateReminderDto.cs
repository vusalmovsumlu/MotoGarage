namespace MotoGarage.Application.DTOs.Reminder;

public class CreateReminderDto
{
    public int VehicleId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime ReminderDate { get; set; }

    public int? ReminderMileage { get; set; }
}