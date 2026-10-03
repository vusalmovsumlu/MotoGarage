namespace MotoGarage.Domain.Entities;

public class ServiceRecord
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public string ServiceName { get; set; }
    public string? Description { get; set; }

    public decimal Cost { get; set; }

    public int Mileage { get; set; }

    public DateTime ServiceDate { get; set; }

    public Vehicle Vehicle { get; set; }
}