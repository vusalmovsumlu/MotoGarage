using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.FuelRecord;

public class CreateFuelRecordDto
{
    public int VehicleId { get; set; }

    [Range(typeof(decimal), "0.01", "1000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Liters { get; set; }

    [Range(typeof(decimal), "0.01", "10000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal PricePerLiter { get; set; }

    [Range(0, 10000000)]
    public int Kilometer { get; set; }

    public DateTime FuelDate { get; set; }
}