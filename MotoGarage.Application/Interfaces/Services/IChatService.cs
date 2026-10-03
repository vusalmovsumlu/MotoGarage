using MotoGarage.Application.DTOs.Chat;

namespace MotoGarage.Application.Interfaces.Services;

public interface IChatService
{
    Task<IEnumerable<ConversationDto>> GetMyConversationsAsync(
        int userId);

    Task<ConversationDto> GetByIdAsync(
        int conversationId,
        int userId);

    Task<IEnumerable<MessageDto>> GetMessagesAsync(
        int conversationId,
        int userId);

    Task<ConversationDto> StartMechanicConversationAsync(
        int userId,
        int mechanicId);

    Task<ConversationDto> StartStoreConversationAsync(
        int userId,
        int storeId);

    Task<MessageDto> SendMessageAsync(
        int conversationId,
        int senderUserId,
        SendMessageDto dto);

    Task MarkAsReadAsync(
        int conversationId,
        int userId);

    Task<int> GetUnreadCountAsync(
        int conversationId,
        int userId);
}