using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ProductRepository
    : Repository<Product>, IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<Product?> GetBySlugAsync(
        string slug)
    {
        return await _context.Products
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x =>
                x.Slug == slug);
    }

    public async Task<IEnumerable<Product>>
        GetByStoreIdAsync(int storeId)
    {
        return await _context.Products.AsNoTracking()
            .Where(x =>
                x.StoreId == storeId &&
                x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(
        string? search,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var query = _context.Products.AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Store.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                (x.Description != null &&
                 x.Description.Contains(search)) ||
                (x.VehicleCompatibility != null &&
                 x.VehicleCompatibility.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            brand = brand.Trim();

            query = query.Where(x =>
                x.Brand != null &&
                x.Brand.Contains(brand));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x =>
                x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x =>
                x.Price <= maxPrice.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product?> GetDetailsByIdAsync(
        int id)
    {
        return await _context.Products
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}