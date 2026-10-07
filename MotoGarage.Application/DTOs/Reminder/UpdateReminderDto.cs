using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Reminder;

public class UpdateReminderDto
{
    [Required, StringLength(150)]
    public string Title { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime ReminderDate { get; set; }

    [Range(0, 10000000)]
    public int? ReminderKm { get; set; }
}