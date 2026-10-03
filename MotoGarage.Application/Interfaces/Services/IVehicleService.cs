using MotoGarage.Application.DTOs.Vehicle;

namespace MotoGarage.Application.Interfaces.Services;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync();

    Task<IEnumerable<VehicleDto>> GetByUserIdAsync(int userId);

    Task<VehicleDto> GetByIdAsync(int id);

    Task<VehicleDto> CreateAsync(
        int userId,
        CreateVehicleDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateVehicleDto dto);

    Task DeleteAsync(
        int id,
        int userId);
}