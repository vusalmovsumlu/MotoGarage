using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ReminderRepository
    : Repository<Reminder>, IReminderRepository
{
    private readonly AppDbContext _context;

    public ReminderRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Reminder>> GetByVehicleIdAsync(
        int vehicleId)
    {
        return await _context.Reminders.AsNoTracking()
            .Where(x => x.VehicleId == vehicleId)
            .OrderBy(x => x.ReminderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetActiveRemindersAsync(
        int vehicleId)
    {
        return await _context.Reminders.AsNoTracking()
            .Where(x =>
                x.VehicleId == vehicleId &&
                !x.IsCompleted)
            .OrderBy(x => x.ReminderDate)
            .ToListAsync();
    }
}