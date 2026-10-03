namespace MotoGarage.Application.DTOs.ServiceRecord;

public class UpdateServiceRecordDto
{
    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Cost { get; set; }

    public int Mileage { get; set; }

    public DateTime ServiceDate { get; set; }
}