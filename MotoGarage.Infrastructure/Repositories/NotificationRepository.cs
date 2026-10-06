using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class NotificationRepository
    : Repository<Notification>,
      INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(
        AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>>
        GetByUserIdAsync(
            int userId,
            bool unreadOnly)
    {
        var query =
            _context.Notifications
                .Where(x => x.UserId == userId)
                .AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(
        int userId)
    {
        return await _context.Notifications
            .CountAsync(x =>
                x.UserId == userId &&
                !x.IsRead);
    }
}