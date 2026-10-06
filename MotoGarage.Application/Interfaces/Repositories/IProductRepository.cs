using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);

    Task<IEnumerable<Product>> GetByStoreIdAsync(
        int storeId);

    Task<IEnumerable<Product>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice);

    Task<Product?> GetDetailsByIdAsync(int id);
}