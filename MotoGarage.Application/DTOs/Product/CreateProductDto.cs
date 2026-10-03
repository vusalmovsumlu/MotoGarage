namespace MotoGarage.Application.DTOs.Product;

public class CreateProductDto
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? Brand { get; set; }

    public string? VehicleCompatibility { get; set; }

    public string? ImageUrl { get; set; }
}