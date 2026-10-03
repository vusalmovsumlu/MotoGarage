namespace MotoGarage.Application.DTOs.Mechanic;

public class CreateMechanicDto
{
    public string BusinessName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public decimal? HourlyRate { get; set; }

    public string? ImageUrl { get; set; }
}