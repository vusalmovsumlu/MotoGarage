using MotoGarage.Application.DTOs.Mechanic;

namespace MotoGarage.Application.Interfaces.Services;

public interface IMechanicService
{
    Task<IEnumerable<MechanicDto>> GetAllAsync();

    Task<MechanicDto> GetBySlugAsync(string slug);

    Task<MechanicDto> GetMyProfileAsync(int userId);

    Task<IEnumerable<MechanicDto>> SearchAsync(
        string? search,
        string? address);

    Task<MechanicDto> CreateAsync(
        int userId,
        CreateMechanicDto dto);

    Task UpdateAsync(
        int userId,
        UpdateMechanicDto dto);

    Task DeleteAsync(int userId);
}