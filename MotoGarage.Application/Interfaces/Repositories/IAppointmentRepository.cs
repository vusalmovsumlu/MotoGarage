using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetByUserIdAsync(int userId);

    Task<IEnumerable<Appointment>> GetByMechanicIdAsync(int mechanicId);

    Task<Appointment?> GetDetailsByIdAsync(int id);
    Task<bool> HasActiveSlotAsync(int mechanicId, DateTime date);
    Task<bool> HasVehicleAppointmentsAsync(int vehicleId);
}