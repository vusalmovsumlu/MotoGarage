using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetByVehicleIdAsync(int vehicleId);

    Task<IEnumerable<Expense>> GetByDateRangeAsync(
        int vehicleId,
        DateTime startDate,
        DateTime endDate);
}