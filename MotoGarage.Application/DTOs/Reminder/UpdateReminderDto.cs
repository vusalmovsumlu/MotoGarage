namespace MotoGarage.Application.DTOs.Reminder;

public class UpdateReminderDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime ReminderDate { get; set; }

    public int? ReminderMileage { get; set; }
}