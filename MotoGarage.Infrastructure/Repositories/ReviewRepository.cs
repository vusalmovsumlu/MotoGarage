using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ReviewRepository
    : Repository<Review>, IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Review>> GetByMechanicIdAsync(
        int mechanicId)
    {
        return await _context.Reviews
            .Include(x => x.User)
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<double> GetAverageRatingAsync(int mechanicId)
    {
        var ratings = await _context.Reviews
            .Where(x => x.MechanicId == mechanicId)
            .Select(x => x.Rating)
            .ToListAsync();

        if (ratings.Count == 0)
            return 0;

        return ratings.Average();
    }

    public async Task<bool> HasUserReviewedAsync(
        int userId,
        int mechanicId)
    {
        return await _context.Reviews
            .AnyAsync(x =>
                x.UserId == userId &&
                x.MechanicId == mechanicId);
    }
}