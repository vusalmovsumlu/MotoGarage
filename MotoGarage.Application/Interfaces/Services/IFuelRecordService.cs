using MotoGarage.Application.DTOs.FuelRecord;

namespace MotoGarage.Application.Interfaces.Services;

public interface IFuelRecordService
{
    Task<IEnumerable<FuelRecordDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId);

    Task<FuelRecordDto> GetByIdAsync(
        int id,
        int userId);

    Task<FuelRecordDto> CreateAsync(
        int userId,
        CreateFuelRecordDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateFuelRecordDto dto);

    Task DeleteAsync(
        int id,
        int userId);
}