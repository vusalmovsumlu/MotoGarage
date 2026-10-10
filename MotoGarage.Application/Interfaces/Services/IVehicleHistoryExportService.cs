using MotoGarage.Application.DTOs.Vehicle;

namespace MotoGarage.Application.Interfaces.Services;

public interface IVehicleHistoryExportService
{
    Task<VehicleHistoryFile> ExportAsync(string slug, int ownerId, CancellationToken cancellationToken = default);
    Task SendAsync(string slug, int ownerId, SendVehicleHistoryDto dto, CancellationToken cancellationToken = default);
}
