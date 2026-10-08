using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Vehicle;

public class VehicleDto
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public int UserId { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int Year { get; set; }

    public string Engine { get; set; } = null!;

    public VehicleType VehicleType { get; set; }

    public FuelType FuelType { get; set; }

    public string VIN { get; set; } = null!;

    public string PlateNumber { get; set; } = null!;

    public int CurrentKm { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public DateTime CreatedAt { get; set; }
}
