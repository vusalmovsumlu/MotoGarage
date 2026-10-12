namespace MotoGarage.Domain.Entities;

public class AdminAuditLog
{
    public long Id { get; set; }
    public int AdminId { get; set; }
    public string AdminName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string TargetType { get; set; } = null!;
    public int TargetId { get; set; }
    public string TargetName { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public User Admin { get; set; } = null!;
}
