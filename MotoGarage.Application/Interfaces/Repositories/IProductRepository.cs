using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByStoreIdAsync(int storeId);

    Task<IEnumerable<Product>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice);

    Task<Product?> GetDetailsByIdAsync(int id);
}