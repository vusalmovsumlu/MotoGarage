using MotoGarage.Application.DTOs.Dashboard;

namespace MotoGarage.Application.Interfaces.Services;

public interface IDashboardQuery
{
    Task<DashboardDto> GetAsync(int userId);
}
