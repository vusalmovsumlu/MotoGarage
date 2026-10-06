namespace MotoGarage.Domain.Entities;

public class Store
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string StoreName { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Product> Products { get; set; }
        = new List<Product>();

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}