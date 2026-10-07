using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Chat;

public class SendMessageDto
{
    [Required, StringLength(2000)]
    public string Content { get; set; } = null!;
}