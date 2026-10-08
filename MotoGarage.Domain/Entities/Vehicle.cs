using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }

    public string Slug { get; set; } = $"qaraj-{Guid.NewGuid():N}";

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int UserId { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int Year { get; set; }

    public string Engine { get; set; } = null!;

    public VehicleType VehicleType { get; set; }

    public FuelType FuelType { get; set; }

    public string VIN { get; set; } = null!;

    public string PlateNumber { get; set; } = null!;

    public int CurrentKm { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<ServiceRecord> ServiceRecords { get; set; }
        = new List<ServiceRecord>();

    public ICollection<FuelRecord> FuelRecords { get; set; }
        = new List<FuelRecord>();

    public ICollection<Expense> Expenses { get; set; }
        = new List<Expense>();

    public ICollection<Reminder> Reminders { get; set; }
        = new List<Reminder>();
}
