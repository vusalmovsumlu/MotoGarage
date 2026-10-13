namespace MotoGarage.Application.DTOs.Chat;

public class ChatAttachmentDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string FileName { get; set; } = "";
    public long SizeBytes { get; set; }
}
