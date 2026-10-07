using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class AppointmentRepository
    : Repository<Appointment>, IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Appointment>> GetByUserIdAsync(int userId)
    {
        return await _context.Appointments.AsNoTracking()
            .Include(x => x.Mechanic)
            .Include(x => x.Vehicle)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByMechanicIdAsync(
        int mechanicId)
    {
        return await _context.Appointments.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Vehicle)
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.AppointmentDate)
            .ToListAsync();
    }

    public async Task<Appointment?> GetDetailsByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(x => x.User)
            .Include(x => x.Mechanic)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    public Task<bool> HasActiveSlotAsync(int mechanicId, DateTime date) =>
        _context.Appointments.AnyAsync(x => x.MechanicId == mechanicId &&
            x.AppointmentDate == date && (x.Status == Domain.Enums.AppointmentStatus.Pending ||
                x.Status == Domain.Enums.AppointmentStatus.Accepted));

    public Task<bool> HasVehicleAppointmentsAsync(int vehicleId) =>
        _context.Appointments.AnyAsync(x => x.VehicleId == vehicleId);
}