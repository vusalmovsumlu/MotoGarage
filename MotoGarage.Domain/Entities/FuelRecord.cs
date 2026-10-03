namespace MotoGarage.Domain.Entities;

public class FuelRecord
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public decimal Liters { get; set; }

    public decimal PricePerLiter { get; set; }

    public decimal TotalPrice { get; set; }

    public int Mileage { get; set; }

    public DateTime FuelDate { get; set; }

    public Vehicle Vehicle { get; set; }
}
