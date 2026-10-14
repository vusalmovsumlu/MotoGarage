using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Infrastructure.Email;

public sealed class WorkflowEmailOptions
{
    public const string SectionName = "WorkflowEmails";
    public bool Enabled { get; set; } = true;
    [Range(1, 3600)] public int PollIntervalSeconds { get; set; } = 10;
    [Range(1, 100)] public int BatchSize { get; set; } = 25;
    [Range(3, 60)] public int LeaseMinutes { get; set; } = 5;
    [Range(10, 90)] public int SendTimeoutSeconds { get; set; } = 90;
    [Range(1, 3600)] public int RetryDelaySeconds { get; set; } = 60;
    [Range(1, 20)] public int MaxAttempts { get; set; } = 10;
}
