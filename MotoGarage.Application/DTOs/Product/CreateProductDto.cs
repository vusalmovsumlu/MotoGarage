using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Product;

public class CreateProductDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Price { get; set; }

    [Range(0, 10000000)]
    public int Stock { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(300)]
    public string? VehicleCompatibility { get; set; }

    [StringLength(2048), Url]
    public string? ImageUrl { get; set; }

    [StringLength(255)]
    public string? ImagePublicId { get; set; }
}