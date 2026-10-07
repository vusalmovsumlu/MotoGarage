namespace MotoGarage.Application.DTOs.FuelRecord;

public class FuelRecordDto
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public decimal Liters { get; set; }

    public decimal PricePerLiter { get; set; }

    public decimal TotalPrice { get; set; }

    public int Kilometer { get; set; }

    public DateTime FuelDate { get; set; }
}