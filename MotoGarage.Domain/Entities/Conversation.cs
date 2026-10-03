namespace MotoGarage.Domain.Entities;

public class Conversation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? MechanicId { get; set; }

    public int? StoreId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public User User { get; set; }

    public Mechanic? Mechanic { get; set; }

    public Store? Store { get; set; }

    public ICollection<Message> Messages { get; set; }
}