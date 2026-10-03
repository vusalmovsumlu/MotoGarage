namespace MotoGarage.Domain.Entities;

public class Store
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string StoreName { get; set; }
    public string Description { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; }

    public ICollection<Product> Products { get; set; }

    public ICollection<Order> Orders { get; set; }
}