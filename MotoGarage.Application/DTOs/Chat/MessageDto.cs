using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Chat;

public class MessageDto
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string Content { get; set; } = null!;
    public ChatAttachmentDto? Attachment { get; set; }

    public DateTime SentAt { get; set; }

    public MessageStatus Status { get; set; }
}
