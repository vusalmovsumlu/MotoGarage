using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Chat;

public class SendMessageDto : IValidatableObject
{
    [StringLength(2000)]
    public string? Content { get; set; }
    public Guid? AttachmentId { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Content) && !AttachmentId.HasValue)
            yield return new("Mesaj və ya şəkil/video tələb olunur.");
        if (AttachmentId == Guid.Empty) yield return new("Fayl məlumatı düzgün deyil.");
    }
}
