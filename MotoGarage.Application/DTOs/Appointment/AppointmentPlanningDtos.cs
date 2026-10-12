using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Application.DTOs.Appointment;

public class WorkingHourDto
{
    [Range(0, 6)] public int DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
}

public record AvailabilityDto(string TimeZone, int DurationMinutes, List<DateTime> Slots);

public class RescheduleAppointmentDto
{
    public DateTime AppointmentDate { get; set; }
}

public class AppointmentDecisionDto
{
    [Range(1, int.MaxValue)] public int Revision { get; set; }
    public bool Approve { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
}

public class SubmitServiceReportDto
{
    [Required, StringLength(100)] public string ServiceName { get; set; } = null!;
    [StringLength(500)] public string? Description { get; set; }
    [StringLength(1000)] public string? ReplacedParts { get; set; }
    [Range(0, 10000000)] public int Kilometer { get; set; }
    [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Cost { get; set; }
    public DateTime ServiceDate { get; set; }
    [Range(0, int.MaxValue)] public int Revision { get; set; }
}

public record ServiceReportDto(int Id, int AppointmentId, string ServiceName, string? Description,
    string? ReplacedParts, int Kilometer, decimal Cost, DateTime ServiceDate, string Status,
    int Revision, string? DecisionComment, DateTime UpdatedAtUtc, DateTime? DecidedAtUtc);
