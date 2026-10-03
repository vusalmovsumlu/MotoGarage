using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ServiceRecordRepository
    : Repository<ServiceRecord>, IServiceRecordRepository
{
    private readonly AppDbContext _context;

    public ServiceRecordRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceRecord>> GetByVehicleIdAsync(
        int vehicleId)
    {
        return await _context.ServiceRecords
            .Where(x => x.VehicleId == vehicleId)
            .ToListAsync();
    }
}