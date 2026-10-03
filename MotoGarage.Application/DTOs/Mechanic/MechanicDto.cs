namespace MotoGarage.Application.DTOs.Mechanic;

public class MechanicDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string BusinessName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public decimal? HourlyRate { get; set; }

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}