using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Order;

public class UpdateOrderStatusDto
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}