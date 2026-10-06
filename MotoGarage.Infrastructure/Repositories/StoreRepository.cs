using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class StoreRepository
    : Repository<Store>, IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Store?> GetByUserIdAsync(int userId)
    {
        return await _context.Stores
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<Store?> GetBySlugAsync(string slug)
    {
        return await _context.Stores
            .FirstOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<IEnumerable<Store>>
        GetActiveStoresAsync()
    {
        return await _context.Stores
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Rating)
            .ToListAsync();
    }

    public async Task<IEnumerable<Store>> SearchAsync(
        string? search,
        string? address)
    {
        var query = _context.Stores
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.StoreName.Contains(search) ||
                x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            address = address.Trim();

            query = query.Where(x =>
                x.Address.Contains(address));
        }

        return await query
            .OrderByDescending(x => x.Rating)
            .ToListAsync();
    }
}