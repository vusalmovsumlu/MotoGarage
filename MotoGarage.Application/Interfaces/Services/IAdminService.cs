using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.Interfaces.Services;

public interface IAdminService
{
    Task<IEnumerable<AdminUserDto>> GetUsersAsync(
        string? search,
        UserRole? role);

    Task<AdminDashboardDto> GetDashboardAsync();

    Task BlockUserAsync(
        int userId,
        int currentAdminId, string? reason = null);

    Task UnblockUserAsync(int userId, int currentAdminId, string? reason = null);

    Task DeleteUserAsync(int userId, int currentAdminId, string? reason = null);
}
