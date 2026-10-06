namespace MotoGarage.Domain.Entities;

public class Mechanic
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string BusinessName { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public decimal? HourlyRate { get; set; }

    public double Rating { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();
}