using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Infrastructure.Notifications;

public sealed class AppointmentEmailReminderOptions
{
    public const string SectionName = "AppointmentEmailReminders";
    public bool Enabled { get; set; } = true;
    [Range(10, 3600)] public int PollIntervalSeconds { get; set; } = 60;
    [Range(1, 24)] public int LeadTimeHours { get; set; } = 2;
    [Range(1, 100)] public int BatchSize { get; set; } = 25;
    [Range(3, 60)] public int LeaseMinutes { get; set; } = 5;
    [Range(1, 60)] public int RetryMinutes { get; set; } = 5;
}
