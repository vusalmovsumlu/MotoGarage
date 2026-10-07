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
    private readonly IImageService _images;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public StoreService(
        IStoreRepository storeRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IImageService images)
    {
        _images = images;
        _unitOfWork = unitOfWork;
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
        return await SlugHelper.RetryAsync(async () =>
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                DtoValidation.Validate(dto);
                var existingStore =
                    await _storeRepository.GetByUserIdAsync(userId);

                if (existingStore is not null)
                {
                    throw new DomainException(
                        "Bu istifadəçi üçün artıq mağaza profili mövcuddur.");
                }
                await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);


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
            });
        }, "IX_Stores_Slug");
    }

    public async Task UpdateAsync(
        int userId,
        UpdateStoreDto dto)
    {
        string? previousImage = null;
        await _unitOfWork.ExecuteAsync(async () =>
        {
            DtoValidation.Validate(dto);
            var store =
                await _storeRepository.GetByUserIdAsync(userId);

            if (store is null)
            {
                throw new KeyNotFoundException(
                    "Mağaza profili tapılmadı.");
            }

            previousImage = store.ImagePublicId;
            if (dto.ImagePublicId is null && dto.ImageUrl == store.ImageUrl)
                dto.ImagePublicId = previousImage;
            await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);
            _mapper.Map(dto, store);



            await _storeRepository.UpdateAsync(store);

            await _storeRepository.SaveChangesAsync();
        });
        await _images.DeleteUnusedAsync(previousImage, userId);
    }

    public async Task DeleteAsync(int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
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
        });
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