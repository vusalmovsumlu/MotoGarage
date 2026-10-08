using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Infrastructure.Notifications;

public class ReminderNotificationOptions
{
    public const string SectionName = "ReminderNotifications";

    [Range(5, 3600)]
    public int PollIntervalSeconds { get; set; } = 30;

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    [Range(1, 100)]
    public int MaxBatchesPerPass { get; set; } = 10;
}
