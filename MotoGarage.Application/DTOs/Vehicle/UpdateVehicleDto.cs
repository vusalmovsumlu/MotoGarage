using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Vehicle;

public class UpdateVehicleDto
{
    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int Year { get; set; }

    public string Engine { get; set; } = null!;

    public VehicleType VehicleType { get; set; }

    public FuelType FuelType { get; set; }

    public string VIN { get; set; } = null!;

    public string PlateNumber { get; set; } = null!;

    public int Mileage { get; set; }

    public string? ImageUrl { get; set; }
}