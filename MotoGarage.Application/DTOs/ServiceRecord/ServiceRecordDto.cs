namespace MotoGarage.Application.DTOs.ServiceRecord;

public class ServiceRecordDto
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Cost { get; set; }

    public int Kilometer { get; set; }

    public DateTime ServiceDate { get; set; }
}