using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<IEnumerable<Vehicle>> GetByUserIdAsync(int userId);
    Task<Vehicle?> GetBySlugAsync(string slug, int userId);
}
