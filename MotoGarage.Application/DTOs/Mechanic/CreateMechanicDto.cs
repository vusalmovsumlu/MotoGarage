using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Mechanic;

public class CreateMechanicDto
{
    [Required, StringLength(150)]
    public string BusinessName { get; set; } = null!;

    [Required, StringLength(500)]
    public string Description { get; set; } = null!;

    [Required, StringLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [Required, StringLength(250)]
    public string Address { get; set; } = null!;

    [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? HourlyRate { get; set; }

    [StringLength(2048), Url]
    public string? ImageUrl { get; set; }

    [StringLength(255)]
    public string? ImagePublicId { get; set; }
}