using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repositories;

public interface IMechanicRepository : IRepository<Mechanic>
{
    Task<Mechanic?> GetByUserIdAsync(int userId);
    Task<Mechanic?> GetBySlugAsync(string slug);
    Task<IEnumerable<Mechanic>> GetActiveMechanicsAsync();

    Task<IEnumerable<Mechanic>> SearchAsync(
        string? search,
        string? address);
}