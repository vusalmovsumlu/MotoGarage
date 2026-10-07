namespace MotoGarage.Domain.Entities;

public class Review
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int MechanicId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Mechanic Mechanic { get; set; } = null!;
}