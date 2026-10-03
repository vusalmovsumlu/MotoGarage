using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int StoreId { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; }

    public Store Store { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
}