using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Vehicle;

public class CreateVehicleDto
{
    [Required, StringLength(50)]
    public string Brand { get; set; } = null!;

    [Required, StringLength(50)]
    public string Model { get; set; } = null!;

    [MotoGarage.Application.Helpers.VehicleYear]
    public int Year { get; set; }

    [Required, StringLength(50)]
    public string Engine { get; set; } = null!;

    [EnumDataType(typeof(VehicleType))]
    public VehicleType VehicleType { get; set; }

    [EnumDataType(typeof(FuelType))]
    public FuelType FuelType { get; set; }

    [Required, StringLength(50)]
    public string VIN { get; set; } = null!;

    [Required, StringLength(20)]
    public string PlateNumber { get; set; } = null!;

    [Range(0, 10000000)]
    public int CurrentKm { get; set; }

    [StringLength(2048), Url]
    public string? ImageUrl { get; set; }

    [StringLength(255)]
    public string? ImagePublicId { get; set; }
}