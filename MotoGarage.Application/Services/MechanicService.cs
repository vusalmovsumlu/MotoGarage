using AutoMapper;
using MotoGarage.Application.DTOs.Mechanic;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class MechanicService : IMechanicService
{
    private readonly IImageService _images;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IMapper _mapper;

    public MechanicService(
        IMechanicRepository mechanicRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IImageService images)
    {
        _images = images;
        _unitOfWork = unitOfWork;
        _mechanicRepository = mechanicRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MechanicDto>> GetAllAsync()
    {
        var mechanics =
            await _mechanicRepository.GetActiveMechanicsAsync();

        return _mapper.Map<IEnumerable<MechanicDto>>(mechanics);
    }

    public async Task<MechanicDto> GetBySlugAsync(
        string slug)
    {
        var mechanic =
            await _mechanicRepository.GetBySlugAsync(slug);

        if (mechanic is null || !mechanic.IsActive)
        {
            throw new KeyNotFoundException(
                "Usta tapılmadı.");
        }

        return _mapper.Map<MechanicDto>(mechanic);
    }

    public async Task<MechanicDto> GetMyProfileAsync(
        int userId)
    {
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta profili tapılmadı.");
        }

        return _mapper.Map<MechanicDto>(mechanic);
    }

    public async Task<IEnumerable<MechanicDto>> SearchAsync(
        string? search,
        string? address)
    {
        var mechanics =
            await _mechanicRepository.SearchAsync(
                search,
                address);

        return _mapper.Map<IEnumerable<MechanicDto>>(mechanics);
    }

    public async Task<MechanicDto> CreateAsync(
        int userId,
        CreateMechanicDto dto)
    {
        return await SlugHelper.RetryAsync(async () =>
        {
            return await _unitOfWork.ExecuteAsync(async () =>
            {
                DtoValidation.Validate(dto);
                var existingMechanic =
                    await _mechanicRepository.GetByUserIdAsync(userId);

                if (existingMechanic is not null)
                {
                    throw new DomainException(
                        "Bu istifadəçi üçün artıq usta profili mövcuddur.");
                }
                await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);


                var mechanic =
                    _mapper.Map<Mechanic>(dto);

                mechanic.UserId = userId;

                mechanic.Slug =
                    await GenerateUniqueSlugAsync(
                        dto.BusinessName);

                mechanic.Rating = 0;
                mechanic.IsActive = true;
                mechanic.CreatedAt = DateTime.UtcNow;

                await _mechanicRepository.AddAsync(mechanic);

                await _mechanicRepository.SaveChangesAsync();

                return _mapper.Map<MechanicDto>(mechanic);
            });
        }, "IX_Mechanics_Slug");
    }

    public async Task UpdateAsync(
        int userId,
        UpdateMechanicDto dto)
    {
        string? previousImage = null;
        await _unitOfWork.ExecuteAsync(async () =>
        {
            DtoValidation.Validate(dto);
            var mechanic =
                await _mechanicRepository.GetByUserIdAsync(userId);

            if (mechanic is null)
            {
                throw new KeyNotFoundException(
                    "Usta profili tapılmadı.");
            }

            previousImage = mechanic.ImagePublicId;
            if (dto.ImagePublicId is null && dto.ImageUrl == mechanic.ImageUrl)
                dto.ImagePublicId = previousImage;
            await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);
            _mapper.Map(dto, mechanic);



            await _mechanicRepository.UpdateAsync(mechanic);

            await _mechanicRepository.SaveChangesAsync();
        });
        await _images.DeleteUnusedAsync(previousImage, userId);
    }

    public async Task DeleteAsync(int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var mechanic =
                await _mechanicRepository.GetByUserIdAsync(userId);

            if (mechanic is null)
            {
                throw new KeyNotFoundException(
                    "Usta profili tapılmadı.");
            }

            mechanic.IsActive = false;

            await _mechanicRepository.UpdateAsync(mechanic);

            await _mechanicRepository.SaveChangesAsync();
        });
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string value,
        int? currentMechanicId = null)
    {
        var baseSlug = SlugHelper.Generate(value);

        var slug = baseSlug;
        var counter = 2;

        while (true)
        {
            var existing =
                await _mechanicRepository
                    .GetBySlugAsync(slug);

            if (existing is null ||
                existing.Id == currentMechanicId)
            {
                return slug;
            }

            slug = $"{baseSlug}-{counter}";
            counter++;
        }
    }
}