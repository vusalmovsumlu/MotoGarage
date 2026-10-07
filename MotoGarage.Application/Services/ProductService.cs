using AutoMapper;
using MotoGarage.Application.DTOs.Product;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class ProductService : IProductService
{
    private readonly IImageService _images;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IImageService images)
    {
        _images = images;
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products =
            await _productRepository.SearchAsync(
                null,
                null,
                null,
                null);

        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> GetBySlugAsync(
        string slug)
    {
        var product =
            await _productRepository.GetBySlugAsync(slug);

        if (product is null ||
            !product.IsActive ||
            !product.Store.IsActive)
        {
            throw new KeyNotFoundException(
                "Məhsul tapılmadı.");
        }

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>>
        GetByStoreSlugAsync(string storeSlug)
    {
        var store =
            await _storeRepository.GetBySlugAsync(
                storeSlug);

        if (store is null || !store.IsActive)
        {
            throw new KeyNotFoundException(
                "Mağaza tapılmadı.");
        }

        var products =
            await _productRepository
                .GetByStoreIdAsync(store.Id);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (minPrice.HasValue &&
            minPrice.Value < 0)
        {
            throw new DomainException(
                "Minimum qiymət mənfi ola bilməz.");
        }

        if (maxPrice.HasValue &&
            maxPrice.Value < 0)
        {
            throw new DomainException(
                "Maksimum qiymət mənfi ola bilməz.");
        }

        if (minPrice.HasValue &&
            maxPrice.HasValue &&
            minPrice.Value > maxPrice.Value)
        {
            throw new DomainException(
                "Minimum qiymət maksimum qiymətdən böyük ola bilməz.");
        }

        var products =
            await _productRepository.SearchAsync(
                search,
                brand,
                minPrice,
                maxPrice);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    public async Task<ProductDto> CreateAsync(
        int storeUserId,
        CreateProductDto dto)
    {
        return await SlugHelper.RetryAsync(async () =>
        {
            DtoValidation.Validate(dto);
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                var store =
                    await GetStoreByUserIdAsync(storeUserId);

                if (dto.Price < 0)
                {
                    throw new DomainException(
                        "Məhsulun qiyməti mənfi ola bilməz.");
                }

                if (dto.Stock < 0)
                {
                    throw new DomainException(
                        "Stok mənfi ola bilməz.");
                }
                await _images.ValidateOwnershipAsync(storeUserId, dto.ImagePublicId, dto.ImageUrl);


                var product =
                    _mapper.Map<Product>(dto);

                product.StoreId = store.Id;

                product.Slug =
                    await GenerateUniqueSlugAsync(
                        dto.Name);

                product.IsActive = true;
                product.CreatedAt = DateTime.UtcNow;

                await _productRepository.AddAsync(product);

                await _productRepository.SaveChangesAsync();

                return _mapper.Map<ProductDto>(product);
            });
        }, "IX_Products_Slug");
    }

    public async Task UpdateAsync(
        string slug,
        int storeUserId,
        UpdateProductDto dto)
    {
        string? previousImage = null;
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var store =
                await GetStoreByUserIdAsync(storeUserId);

            var product =
                await _productRepository.GetBySlugAsync(slug);

            if (product is null)
            {
                throw new KeyNotFoundException(
                    "Məhsul tapılmadı.");
            }

            ValidateOwnership(product, store.Id);

            if (dto.Price < 0)
            {
                throw new DomainException(
                    "Məhsulun qiyməti mənfi ola bilməz.");
            }

            previousImage = product.ImagePublicId;
            if (dto.ImagePublicId is null && dto.ImageUrl == product.ImageUrl)
                dto.ImagePublicId = previousImage;
            await _images.ValidateOwnershipAsync(storeUserId, dto.ImagePublicId, dto.ImageUrl);
            _mapper.Map(dto, product);



            await _productRepository.UpdateAsync(product);

            await _productRepository.SaveChangesAsync();
        });
        await _images.DeleteUnusedAsync(previousImage, storeUserId);
    }

    public async Task UpdateStockAsync(
        string slug,
        int storeUserId,
        UpdateStockDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var store =
                await GetStoreByUserIdAsync(storeUserId);

            var product =
                await _productRepository.GetBySlugAsync(slug);

            if (product is null)
            {
                throw new KeyNotFoundException(
                    "Məhsul tapılmadı.");
            }

            ValidateOwnership(product, store.Id);

            if (dto.Stock < 0)
            {
                throw new DomainException(
                    "Stok mənfi ola bilməz.");
            }

            product.Stock = dto.Stock;

            await _productRepository.UpdateAsync(product);

            await _productRepository.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(
        string slug,
        int storeUserId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var store =
                await GetStoreByUserIdAsync(storeUserId);

            var product =
                await _productRepository.GetBySlugAsync(slug);

            if (product is null)
            {
                throw new KeyNotFoundException(
                    "Məhsul tapılmadı.");
            }

            ValidateOwnership(product, store.Id);

            product.IsActive = false;

            await _productRepository.UpdateAsync(product);

            await _productRepository.SaveChangesAsync();
        });
    }

    private async Task<Store> GetStoreByUserIdAsync(
        int userId)
    {
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is null || !store.IsActive)
        {
            throw new ForbiddenException(
                "Aktiv mağaza profilin yoxdur.");
        }

        return store;
    }

    private static void ValidateOwnership(
        Product product,
        int storeId)
    {
        if (product.StoreId != storeId)
        {
            throw new ForbiddenException(
                "Bu məhsulu idarə etmək icazən yoxdur.");
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string value,
        int? currentProductId = null)
    {
        var baseSlug =
            SlugHelper.Generate(value);

        var slug = baseSlug;
        var counter = 2;

        while (true)
        {
            var existing =
                await _productRepository
                    .GetBySlugAsync(slug);

            if (existing is null ||
                existing.Id == currentProductId)
            {
                return slug;
            }

            slug = $"{baseSlug}-{counter}";
            counter++;
        }
    }
}