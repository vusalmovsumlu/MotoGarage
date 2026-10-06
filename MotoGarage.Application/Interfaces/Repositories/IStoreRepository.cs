using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repositories;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetByUserIdAsync(int userId);
    Task<Store?> GetBySlugAsync(string slug);
    Task<IEnumerable<Store>> GetActiveStoresAsync();

    Task<IEnumerable<Store>> SearchAsync(
        string? search,
        string? address);
}