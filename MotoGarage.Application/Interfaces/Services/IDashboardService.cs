using MotoGarage.Application.DTOs.Dashboard;

namespace MotoGarage.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int userId);
}