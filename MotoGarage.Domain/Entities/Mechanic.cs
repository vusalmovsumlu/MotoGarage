namespace MotoGarage.Domain.Entities;

public class Mechanic
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string BusinessName { get; set; }
    public string Description { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }

    public decimal? HourlyRate { get; set; }

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public User User { get; set; }

    public ICollection<Appointment> Appointments { get; set; }

    public ICollection<Review> Reviews { get; set; }
}