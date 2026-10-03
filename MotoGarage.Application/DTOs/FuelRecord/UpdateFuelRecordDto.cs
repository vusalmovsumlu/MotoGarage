namespace MotoGarage.Application.DTOs.FuelRecord;

public class UpdateFuelRecordDto
{
    public decimal Liters { get; set; }
    public decimal PricePerLiter { get; set; }
    public int Mileage { get; set; }
    public DateTime FuelDate { get; set; }
}