using AutoMapper;
using MotoGarage.Application.DTOs.Store;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public StoreService(
        IStoreRepository storeRepository,
        IMapper mapper)
    {
        _storeRepository = storeRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StoreDto>> GetAllAsync()
    {
        var stores =
            await _storeRepository.GetActiveStoresAsync();

        return _mapper.Map<IEnumerable<StoreDto>>(stores);
    }

    public async Task<StoreDto> GetBySlugAsync(string slug)
    {
        var store =
            await _storeRepository.GetBySlugAsync(slug);

        if (store is null || !store.IsActive)
        {
            throw new KeyNotFoundException(
                "Mağaza tapılmadı.");
        }

        return _mapper.Map<StoreDto>(store);
    }

    public async Task<StoreDto> GetMyProfileAsync(int userId)
    {
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is null)
        {
            throw new KeyNotFoundException(
                "Mağaza profili tapılmadı.");
        }

        return _mapper.Map<StoreDto>(store);
    }

    public async Task<IEnumerable<StoreDto>> SearchAsync(
        string? search,
        string? address)
    {
        var stores =
            await _storeRepository.SearchAsync(
                search,
                address);

        return _mapper.Map<IEnumerable<StoreDto>>(stores);
    }

    public async Task<StoreDto> CreateAsync(
        int userId,
        CreateStoreDto dto)
    {
        var existingStore =
            await _storeRepository.GetByUserIdAsync(userId);

        if (existingStore is not null)
        {
            throw new DomainException(
                "Bu istifadəçi üçün artıq mağaza profili mövcuddur.");
        }

        var store =
            _mapper.Map<Store>(dto);

        store.UserId = userId;

        store.Slug =
            await GenerateUniqueSlugAsync(
                dto.StoreName);

        store.Rating = 0;
        store.IsActive = true;
        store.CreatedAt = DateTime.UtcNow;

        await _storeRepository.AddAsync(store);

        await _storeRepository.SaveChangesAsync();

        return _mapper.Map<StoreDto>(store);
    }

    public async Task UpdateAsync(
        int userId,
        UpdateStoreDto dto)
    {
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is null)
        {
            throw new KeyNotFoundException(
                "Mağaza profili tapılmadı.");
        }

        var oldStoreName = store.StoreName;

        _mapper.Map(dto, store);

        if (!string.Equals(
                oldStoreName,
                dto.StoreName,
                StringComparison.OrdinalIgnoreCase))
        {
            store.Slug =
                await GenerateUniqueSlugAsync(
                    dto.StoreName,
                    store.Id);
        }

        await _storeRepository.UpdateAsync(store);

        await _storeRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId)
    {
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is null)
        {
            throw new KeyNotFoundException(
                "Mağaza profili tapılmadı.");
        }

        store.IsActive = false;

        await _storeRepository.UpdateAsync(store);

        await _storeRepository.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string value,
        int? currentStoreId = null)
    {
        var baseSlug = SlugHelper.Generate(value);

        var slug = baseSlug;
        var counter = 2;

        while (true)
        {
            var existing =
                await _storeRepository.GetBySlugAsync(slug);

            if (existing is null ||
                existing.Id == currentStoreId)
            {
                return slug;
            }

            slug = $"{baseSlug}-{counter}";
            counter++;
        }
    }
}