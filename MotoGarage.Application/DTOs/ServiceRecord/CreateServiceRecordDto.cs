using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.ServiceRecord;

public class CreateServiceRecordDto
{
    public int VehicleId { get; set; }

    [Required, StringLength(100)]
    public string ServiceName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Cost { get; set; }

    [Range(0, 10000000)]
    public int Kilometer { get; set; }

    public DateTime ServiceDate { get; set; }
}