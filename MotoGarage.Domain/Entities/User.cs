using Microsoft.AspNetCore.Identity;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class User : IdentityUser<int>
{
    public string FullName { get; set; } = null!;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; }
        = new List<Vehicle>();

    public Mechanic? Mechanic { get; set; }

    public Store? Store { get; set; }

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();

    public ICollection<Conversation> Conversations { get; set; }
        = new List<Conversation>();

    public ICollection<Message> Messages { get; set; }
        = new List<Message>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}