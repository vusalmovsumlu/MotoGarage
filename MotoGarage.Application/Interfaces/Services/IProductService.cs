using MotoGarage.Application.DTOs.Product;

namespace MotoGarage.Application.Interfaces.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();

    Task<ProductDto> GetByIdAsync(int id);

    Task<IEnumerable<ProductDto>> GetByStoreIdAsync(int storeId);

    Task<IEnumerable<ProductDto>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice);

    Task<ProductDto> CreateAsync(
        int storeUserId,
        CreateProductDto dto);

    Task UpdateAsync(
        int id,
        int storeUserId,
        UpdateProductDto dto);

    Task UpdateStockAsync(
        int id,
        int storeUserId,
        UpdateStockDto dto);

    Task DeleteAsync(
        int id,
        int storeUserId);
}