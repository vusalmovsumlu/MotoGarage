namespace MotoGarage.Domain.Entities;

public class ChatAttachment
{
    public Guid Id { get; set; }
    public int ConversationId { get; set; }
    public int UploadedByUserId { get; set; }
    public string PublicId { get; set; } = "";
    public string Format { get; set; } = "";
    public string Kind { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsAttached { get; set; }
    public bool IsDeleting { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public Conversation Conversation { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
