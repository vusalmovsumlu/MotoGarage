using AutoMapper;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
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
        var vehicle =
            _mapper.Map<Vehicle>(dto);

        vehicle.UserId = userId;
        vehicle.CreatedAt = DateTime.UtcNow;

        await _vehicleRepository.AddAsync(vehicle);

        await _vehicleRepository.SaveChangesAsync();

        return _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateVehicleDto dto)
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
            throw new UnauthorizedAccessException(
                "Bu nəqliyyat vasitəsini dəyişmək icazən yoxdur."
            );
        }

        _mapper.Map(dto, vehicle);

        await _vehicleRepository.UpdateAsync(vehicle);

        await _vehicleRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        int id,
        int userId)
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
            throw new UnauthorizedAccessException(
                "Bu nəqliyyat vasitəsini silmək icazən yoxdur."
            );
        }

        await _vehicleRepository.DeleteAsync(vehicle);

        await _vehicleRepository.SaveChangesAsync();
    }
}