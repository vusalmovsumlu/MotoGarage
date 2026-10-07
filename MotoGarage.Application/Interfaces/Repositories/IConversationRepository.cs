using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId);

    Task<IEnumerable<Conversation>> GetByMechanicIdAsync(int mechanicId);

    Task<IEnumerable<Conversation>> GetByStoreIdAsync(int storeId);

    Task<Conversation?> GetParticipantsAsync(int conversationId);

    Task<Conversation?> GetWithMessagesAsync(int conversationId);

    Task<Conversation?> GetUserMechanicConversationAsync(
        int userId,
        int mechanicId);

    Task<Conversation?> GetUserStoreConversationAsync(
        int userId,
        int storeId);
}