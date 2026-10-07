using MotoGarage.Application.DTOs.Dashboard;
using MotoGarage.Application.Interfaces.Services;

namespace MotoGarage.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardQuery _query;

    public DashboardService(IDashboardQuery query)
    {
        _query = query;
    }

    public Task<DashboardDto> GetDashboardAsync(int userId) => _query.GetAsync(userId);
}
