using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repositories;

public interface INotificationRepository
    : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(
        int userId,
        bool unreadOnly);

    Task<int> GetUnreadCountAsync(int userId);
}