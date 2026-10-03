namespace MotoGarage.Application.DTOs.Store;

public class StoreDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string StoreName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}