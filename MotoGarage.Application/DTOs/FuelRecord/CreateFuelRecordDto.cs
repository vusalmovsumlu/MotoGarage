namespace MotoGarage.Application.DTOs.FuelRecord;

public class CreateFuelRecordDto
{
    public int VehicleId { get; set; }
    public decimal Liters { get; set; }
    public decimal PricePerLiter { get; set; }
    public int Mileage { get; set; }
    public DateTime FuelDate { get; set; }
}