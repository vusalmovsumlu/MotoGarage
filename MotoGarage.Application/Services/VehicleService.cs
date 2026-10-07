using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Exceptions;
using AutoMapper;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IImageService _images;
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IAppointmentRepository appointments,
        IImageService images)
    {
        _images = images;
        _appointments = appointments;
        _unitOfWork = unitOfWork;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        var vehicles =
            await _vehicleRepository.GetAllAsync();

        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }

    public async Task<IEnumerable<VehicleDto>> GetByUserIdAsync(
        int userId)
    {
        var vehicles =
            await _vehicleRepository.GetByUserIdAsync(userId);

        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }

    public async Task<VehicleDto> GetByIdAsync(int id)
    {
        var vehicle =
            await _vehicleRepository.GetByIdAsync(id);

        if (vehicle is null)
        {
            throw new KeyNotFoundException(
                "Nəqliyyat vasitəsi tapılmadı."
            );
        }

        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> CreateAsync(
        int userId,
        CreateVehicleDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);

            var vehicle =
                _mapper.Map<Vehicle>(dto);

            vehicle.UserId = userId;
            vehicle.CreatedAt = DateTime.UtcNow;

            await _vehicleRepository.AddAsync(vehicle);

            await _vehicleRepository.SaveChangesAsync();

            return _mapper.Map<VehicleDto>(vehicle);
        });
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateVehicleDto dto)
    {
        string? previousImage = null;
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var vehicle =
                await _vehicleRepository.GetByIdAsync(id);

            if (vehicle is null)
            {
                throw new KeyNotFoundException(
                    "Nəqliyyat vasitəsi tapılmadı."
                );
            }

            if (vehicle.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu nəqliyyat vasitəsini dəyişmək icazən yoxdur."
                );
            }

            if (dto.CurrentKm < vehicle.CurrentKm)
                throw new DomainException("Cari kilometr azaldıla bilməz.");
            previousImage = vehicle.ImagePublicId;
            if (dto.ImagePublicId is null && dto.ImageUrl == vehicle.ImageUrl)
                dto.ImagePublicId = previousImage;
            await _images.ValidateOwnershipAsync(userId, dto.ImagePublicId, dto.ImageUrl);
            _mapper.Map(dto, vehicle);

            await _vehicleRepository.UpdateAsync(vehicle);

            await _vehicleRepository.SaveChangesAsync();
        });
        await _images.DeleteUnusedAsync(previousImage, userId);
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var vehicle =
                await _vehicleRepository.GetByIdAsync(id);

            if (vehicle is null)
            {
                throw new KeyNotFoundException(
                    "Nəqliyyat vasitəsi tapılmadı."
                );
            }

            if (vehicle.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu nəqliyyat vasitəsini silmək icazən yoxdur."
                );
            }

            if (await _appointments.HasVehicleAppointmentsAsync(id))
                throw new ConflictException("Randevusu olan nəqliyyat vasitəsi silinə bilməz.");
            await _vehicleRepository.DeleteAsync(vehicle);

            await _vehicleRepository.SaveChangesAsync();
        });
    }
}
