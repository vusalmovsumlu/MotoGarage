using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetByConversationIdAsync(
        int conversationId);

    Task<IEnumerable<Message>> GetUnreadMessagesAsync(
        int conversationId,
        int userId);

    Task<int> GetUnreadCountAsync(
        int conversationId,
        int userId);
}