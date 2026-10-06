using MotoGarage.Application.DTOs.Product;

namespace MotoGarage.Application.Interfaces.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();

    Task<ProductDto> GetBySlugAsync(string slug);

    Task<IEnumerable<ProductDto>> GetByStoreSlugAsync(
        string storeSlug);

    Task<IEnumerable<ProductDto>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice);

    Task<ProductDto> CreateAsync(
        int storeUserId,
        CreateProductDto dto);

    Task UpdateAsync(
        string slug,
        int storeUserId,
        UpdateProductDto dto);

    Task UpdateStockAsync(
        string slug,
        int storeUserId,
        UpdateStockDto dto);

    Task DeleteAsync(
        string slug,
        int storeUserId);
}