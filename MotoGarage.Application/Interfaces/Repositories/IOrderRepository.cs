using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByUserIdAsync(int userId);

    Task<IEnumerable<Order>> GetByStoreIdAsync(int storeId);

    Task<Order?> GetDetailsByIdAsync(int id);
}