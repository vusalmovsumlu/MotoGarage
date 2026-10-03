using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ExpenseRepository
    : Repository<Expense>, IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Expense>> GetByVehicleIdAsync(
        int vehicleId)
    {
        return await _context.Expenses
            .Where(x => x.VehicleId == vehicleId)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetByDateRangeAsync(
        int vehicleId,
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.Expenses
            .Where(x =>
                x.VehicleId == vehicleId &&
                x.ExpenseDate >= startDate &&
                x.ExpenseDate <= endDate)
            .OrderByDescending(x => x.ExpenseDate)
            .ToListAsync();
    }
}