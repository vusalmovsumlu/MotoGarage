using MotoGarage.Application.DTOs.ServiceRecord;

namespace MotoGarage.Application.Interfaces.Services;

public interface IServiceRecordService
{
    Task<IEnumerable<ServiceRecordDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId);

    Task<ServiceRecordDto> GetByIdAsync(
        int id,
        int userId);

    Task<ServiceRecordDto> CreateAsync(
        int userId,
        CreateServiceRecordDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateServiceRecordDto dto);

    Task DeleteAsync(
        int id,
        int userId);
}