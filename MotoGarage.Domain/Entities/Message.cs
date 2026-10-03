using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string Content { get; set; }

    public DateTime SentAt { get; set; }

    public MessageStatus Status { get; set; }

    public Conversation Conversation { get; set; }

    public User SenderUser { get; set; }
}