using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IMechanicRepository : IRepository<Mechanic>
{
    Task<Mechanic?> GetByUserIdAsync(int userId);

    Task<IEnumerable<Mechanic>> GetActiveMechanicsAsync();

    Task<IEnumerable<Mechanic>> SearchAsync(
        string? search,
        string? address);
}