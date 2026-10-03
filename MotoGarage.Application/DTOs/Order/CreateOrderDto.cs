namespace MotoGarage.Application.DTOs.Order;

public class CreateOrderDto
{
    public List<CreateOrderItemDto> Items { get; set; }
        = new();
}