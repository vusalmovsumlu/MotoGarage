using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Notifications;

namespace MotoGarage.Infrastructure.Repositories;

public class NotificationRepository
    : Repository<Notification>,
      INotificationRepository
{
    private readonly AppDbContext _context;
    private readonly NotificationChangeInterceptor? _changes;

    public NotificationRepository(
        AppDbContext context, NotificationChangeInterceptor? changes = null)
        : base(context)
    {
        _context = context;
        _changes = changes;
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

    public async Task DeleteByUserIdAsync(int userId)
    {
        var deleted = await _context.Notifications.Where(x => x.UserId == userId).ExecuteDeleteAsync();
        if (deleted > 0) _changes?.Changed(userId, _context);
    }
}
