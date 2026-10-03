using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Order;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}