namespace MotoGarage.Application.DTOs.Order;

public class CheckoutDto
{
    public IReadOnlyList<OrderDto> Orders { get; set; } = [];

    public decimal TotalAmount { get; set; }
}
