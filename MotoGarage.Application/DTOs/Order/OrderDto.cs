using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Order;

public class OrderDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int StoreId { get; set; }

    public string? StoreName { get; set; }

    public string? RecipientName { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryCity { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNote { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public IEnumerable<OrderItemDto> Items { get; set; }
        = new List<OrderItemDto>();
}
