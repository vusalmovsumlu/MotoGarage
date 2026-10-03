namespace MotoGarage.Application.DTOs.Store;

public class CreateStoreDto
{
    public string StoreName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? ImageUrl { get; set; }
}