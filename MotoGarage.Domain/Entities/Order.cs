using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int UserId { get; set; }

    public int StoreId { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public User User { get; set; } = null!;

    public Store Store { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}