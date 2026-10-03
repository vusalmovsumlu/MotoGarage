using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class FuelRecordRepository
    : Repository<FuelRecord>, IFuelRecordRepository
{
    private readonly AppDbContext _context;

    public FuelRecordRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FuelRecord>> GetByVehicleIdAsync(
        int vehicleId)
    {
        return await _context.FuelRecords
            .Where(x => x.VehicleId == vehicleId)
            .ToListAsync();
    }
}