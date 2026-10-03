using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Vehicle
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Brand { get; set; }
    public string Model { get; set; }

    public int Year { get; set; }

    public string Engine { get; set; }

    public VehicleType VehicleType { get; set; }
    public FuelType FuelType { get; set; }

    public string VIN { get; set; }
    public string PlateNumber { get; set; }

    public int Mileage { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }


    public User User { get; set; }

    public ICollection<ServiceRecord> ServiceRecords { get; set; }

    public ICollection<FuelRecord> FuelRecords { get; set; }

    public ICollection<Expense> Expenses { get; set; }

    public ICollection<Reminder> Reminders { get; set; }
}