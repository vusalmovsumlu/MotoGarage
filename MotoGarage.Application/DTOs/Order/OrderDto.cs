using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Order;

public class OrderDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int StoreId { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public IEnumerable<OrderItemDto> Items { get; set; }
        = new List<OrderItemDto>();
}