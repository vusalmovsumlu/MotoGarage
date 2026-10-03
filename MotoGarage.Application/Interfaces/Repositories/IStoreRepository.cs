using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetByUserIdAsync(int userId);

    Task<IEnumerable<Store>> GetActiveStoresAsync();

    Task<IEnumerable<Store>> SearchAsync(
        string? search,
        string? address);
}