using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class OrderRepository
    : Repository<Order>, IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Include(x => x.Store)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByStoreIdAsync(int storeId)
    {
        return await _context.Orders
            .Include(x => x.User)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
            .Where(x => x.StoreId == storeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetDetailsByIdAsync(int id)
    {
        return await _context.Orders
            .Include(x => x.User)
            .Include(x => x.Store)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}