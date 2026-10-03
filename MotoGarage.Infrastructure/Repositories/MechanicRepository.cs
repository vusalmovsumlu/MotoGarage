using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class MechanicRepository
    : Repository<Mechanic>, IMechanicRepository
{
    private readonly AppDbContext _context;

    public MechanicRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Mechanic?> GetByUserIdAsync(int userId)
    {
        return await _context.Mechanics
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<IEnumerable<Mechanic>> GetActiveMechanicsAsync()
    {
        return await _context.Mechanics
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Rating)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mechanic>> SearchAsync(
        string? search,
        string? address)
    {
        var query = _context.Mechanics
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.BusinessName.Contains(search) ||
                x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            query = query.Where(x =>
                x.Address.Contains(address));
        }

        return await query
            .OrderByDescending(x => x.Rating)
            .ToListAsync();
    }
}