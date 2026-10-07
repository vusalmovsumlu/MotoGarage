using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Store;

public class CreateStoreDto
{
    [Required, StringLength(150)]
    public string StoreName { get; set; } = null!;

    [Required, StringLength(500)]
    public string Description { get; set; } = null!;

    [Required, StringLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required, StringLength(250)]
    public string Address { get; set; } = null!;

    [StringLength(2048), Url]
    public string? ImageUrl { get; set; }

    [StringLength(255)]
    public string? ImagePublicId { get; set; }
}