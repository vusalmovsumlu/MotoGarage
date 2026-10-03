using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IServiceRecordRepository : IRepository<ServiceRecord>
{
    Task<IEnumerable<ServiceRecord>> GetByVehicleIdAsync(int vehicleId);
}