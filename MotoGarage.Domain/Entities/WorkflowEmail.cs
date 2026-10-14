namespace MotoGarage.Domain.Entities;

// A committed business event and its immutable email snapshot. SMTP runs later.
public class WorkflowEmail
{
    public long Id { get; set; }
    public Guid DeliveryId { get; set; } = Guid.NewGuid();
    public string EventKey { get; set; } = null!;
    public int RecipientUserId { get; set; }
    public User Recipient { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string TextBody { get; set; } = null!;
    public string HtmlBody { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? DiscardedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public int Attempts { get; set; }
}
