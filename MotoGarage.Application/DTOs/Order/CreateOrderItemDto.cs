using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Order;

public class CreateOrderItemDto
{
    [Range(1, 2147483647)]
    public int ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; }
}