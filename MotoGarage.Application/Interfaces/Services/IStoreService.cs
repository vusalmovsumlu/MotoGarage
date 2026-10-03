using MotoGarage.Application.DTOs.Store;

namespace MotoGarage.Application.Interfaces.Services;

public interface IStoreService
{
    Task<IEnumerable<StoreDto>> GetAllAsync();

    Task<StoreDto> GetByIdAsync(int id);

    Task<StoreDto> GetMyProfileAsync(int userId);

    Task<IEnumerable<StoreDto>> SearchAsync(
        string? search,
        string? address);

    Task<StoreDto> CreateAsync(
        int userId,
        CreateStoreDto dto);

    Task UpdateAsync(
        int userId,
        UpdateStoreDto dto);

    Task DeleteAsync(int userId);
}