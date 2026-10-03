namespace MotoGarage.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public int StoreId { get; set; }

    public string Name { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? Brand { get; set; }

    public string? VehicleCompatibility { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Store Store { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
}