using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.Interfaces.Services;

public interface IBusinessApplicationService
{
    Task<BusinessApplicationDto?> GetMineAsync(int userId);
    Task<List<BusinessApplicationDto>> GetApplicationsAsync(BusinessApprovalStatus? status);
    Task ReviewAsync(int id, int adminId, bool approve, string? comment);
}
