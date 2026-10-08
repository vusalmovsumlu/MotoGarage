using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Vehicle>> GetByUserIdAsync(int userId)
    {
        return await _context.Vehicles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public Task<Vehicle?> GetBySlugAsync(string slug, int userId) =>
        _context.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && x.UserId == userId);
}
