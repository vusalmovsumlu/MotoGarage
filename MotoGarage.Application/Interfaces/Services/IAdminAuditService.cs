using MotoGarage.Application.DTOs.Admin;
namespace MotoGarage.Application.Interfaces.Services;

public interface IAdminAuditService
{
    Task<AdminAuditPage> GetAsync(int adminId, int page, int pageSize, string? action);
}
