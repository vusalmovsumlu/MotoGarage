using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IFuelRecordRepository : IRepository<FuelRecord>
{
    Task<IEnumerable<FuelRecord>> GetByVehicleIdAsync(int vehicleId);
    Task<IEnumerable<FuelRecord>> GetByDateRangeAsync(int vehicleId, DateTime start, DateTime endExclusive);
}