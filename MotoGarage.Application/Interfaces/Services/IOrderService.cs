using MotoGarage.Application.DTOs.Order;

namespace MotoGarage.Application.Interfaces.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetMyOrdersAsync(
        int userId);

    Task<IEnumerable<OrderDto>> GetStoreOrdersAsync(
        int storeUserId);

    Task<OrderDto> GetByIdAsync(
        int id,
        int currentUserId);

    Task<OrderDto> CreateAsync(
        int userId,
        CreateOrderDto dto);

    Task UpdateStatusAsync(
        int orderId,
        int storeUserId,
        UpdateOrderStatusDto dto);

    Task CancelAsync(
        int orderId,
        int userId);
}