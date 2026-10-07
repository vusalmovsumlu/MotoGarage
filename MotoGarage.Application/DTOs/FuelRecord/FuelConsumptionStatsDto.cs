namespace MotoGarage.Application.DTOs.FuelRecord;

public class FuelConsumptionStatsDto
{
    public int VehicleId { get; set; }

    public int RecordCount { get; set; }

    public int StartKm { get; set; }

    public int EndKm { get; set; }

    public int DistanceKm { get; set; }

    public decimal TotalLiters { get; set; }

    public decimal TotalFuelCost { get; set; }

    public decimal AverageConsumptionPer100Km { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}