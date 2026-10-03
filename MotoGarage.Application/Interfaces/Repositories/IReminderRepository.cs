using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IReminderRepository : IRepository<Reminder>
{
    Task<IEnumerable<Reminder>> GetByVehicleIdAsync(int vehicleId);

    Task<IEnumerable<Reminder>> GetActiveRemindersAsync(int vehicleId);
}