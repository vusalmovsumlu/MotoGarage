using AutoMapper;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public ChatService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMechanicRepository mechanicRepository,
        IStoreRepository storeRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _mechanicRepository = mechanicRepository;
        _storeRepository = storeRepository;
        _mapper = mapper;
    }

    // =========================
    // My conversations
    // =========================

    public async Task<IEnumerable<ConversationDto>>
        GetMyConversationsAsync(int userId)
    {
        var conversations = new List<Conversation>();

        // Adi user kimi olan conversation-lar
        var userConversations =
            await _conversationRepository.GetByUserIdAsync(userId);

        conversations.AddRange(userConversations);

        // Əgər user həm də ustadırsa
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (mechanic is not null)
        {
            var mechanicConversations =
                await _conversationRepository
                    .GetByMechanicIdAsync(mechanic.Id);

            conversations.AddRange(mechanicConversations);
        }

        // Əgər user mağaza sahibidirsə
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is not null)
        {
            var storeConversations =
                await _conversationRepository
                    .GetByStoreIdAsync(store.Id);

            conversations.AddRange(storeConversations);
        }

        var result = conversations
            .DistinctBy(x => x.Id)
            .OrderByDescending(x =>
                x.LastMessageAt ?? x.CreatedAt);

        return _mapper.Map<IEnumerable<ConversationDto>>(result);
    }

    // =========================
    // Conversation details
    // =========================

    public async Task<ConversationDto> GetByIdAsync(
        int conversationId,
        int userId)
    {
        var conversation =
            await GetAuthorizedConversationAsync(
                conversationId,
                userId);

        return _mapper.Map<ConversationDto>(conversation);
    }

    // =========================
    // Messages
    // =========================

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(
        int conversationId,
        int userId)
    {
        await GetAuthorizedConversationAsync(
            conversationId,
            userId);

        var messages =
            await _messageRepository
                .GetByConversationIdAsync(conversationId);

        return _mapper.Map<IEnumerable<MessageDto>>(messages);
    }

    // =========================
    // User <-> Mechanic
    // =========================

    public async Task<ConversationDto>
        StartMechanicConversationAsync(
            int userId,
            int mechanicId)
    {
        var mechanic =
            await _mechanicRepository.GetByIdAsync(mechanicId);

        if (mechanic is null || !mechanic.IsActive)
        {
            throw new KeyNotFoundException(
                "Usta tapılmadı.");
        }

        if (mechanic.UserId == userId)
        {
            throw new DomainException(
                "Özünlə söhbət yarada bilməzsən.");
        }

        var existingConversation =
            await _conversationRepository
                .GetUserMechanicConversationAsync(
                    userId,
                    mechanicId);

        if (existingConversation is not null)
        {
            return _mapper.Map<ConversationDto>(
                existingConversation);
        }

        var conversation = new Conversation
        {
            UserId = userId,
            MechanicId = mechanicId,
            StoreId = null,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation);

        await _conversationRepository.SaveChangesAsync();

        return _mapper.Map<ConversationDto>(conversation);
    }

    // =========================
    // User <-> Store
    // =========================

    public async Task<ConversationDto>
        StartStoreConversationAsync(
            int userId,
            int storeId)
    {
        var store =
            await _storeRepository.GetByIdAsync(storeId);

        if (store is null || !store.IsActive)
        {
            throw new KeyNotFoundException(
                "Mağaza tapılmadı.");
        }

        if (store.UserId == userId)
        {
            throw new DomainException(
                "Öz mağazanla söhbət yarada bilməzsən.");
        }

        var existingConversation =
            await _conversationRepository
                .GetUserStoreConversationAsync(
                    userId,
                    storeId);

        if (existingConversation is not null)
        {
            return _mapper.Map<ConversationDto>(
                existingConversation);
        }

        var conversation = new Conversation
        {
            UserId = userId,
            MechanicId = null,
            StoreId = storeId,
            CreatedAt = DateTime.UtcNow
        };

        await _conversationRepository.AddAsync(conversation);

        await _conversationRepository.SaveChangesAsync();

        return _mapper.Map<ConversationDto>(conversation);
    }

    // =========================
    // Send message
    // =========================

    public async Task<MessageDto> SendMessageAsync(
        int conversationId,
        int senderUserId,
        SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new DomainException(
                "Mesaj boş ola bilməz.");
        }

        var conversation =
            await GetAuthorizedConversationAsync(
                conversationId,
                senderUserId);

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderUserId = senderUserId,
            Content = dto.Content.Trim(),
            SentAt = DateTime.UtcNow,
            Status = MessageStatus.Sent
        };

        conversation.LastMessageAt = DateTime.UtcNow;

        await _messageRepository.AddAsync(message);

        await _conversationRepository.UpdateAsync(
            conversation);

        // Hər iki repository eyni scoped DbContext istifadə edir.
        await _messageRepository.SaveChangesAsync();

        return _mapper.Map<MessageDto>(message);
    }

    // =========================
    // Mark as read
    // =========================

    public async Task MarkAsReadAsync(
        int conversationId,
        int userId)
    {
        await GetAuthorizedConversationAsync(
            conversationId,
            userId);

        var unreadMessages =
            await _messageRepository.GetUnreadMessagesAsync(
                conversationId,
                userId);

        foreach (var message in unreadMessages)
        {
            message.Status = MessageStatus.Read;

            await _messageRepository.UpdateAsync(message);
        }

        await _messageRepository.SaveChangesAsync();
    }

    // =========================
    // Unread count
    // =========================

    public async Task<int> GetUnreadCountAsync(
        int conversationId,
        int userId)
    {
        await GetAuthorizedConversationAsync(
            conversationId,
            userId);

        return await _messageRepository.GetUnreadCountAsync(
            conversationId,
            userId);
    }

    // =========================
    // Authorization helper
    // =========================

    private async Task<Conversation>
        GetAuthorizedConversationAsync(
            int conversationId,
            int userId)
    {
        var conversation =
            await _conversationRepository
                .GetWithMessagesAsync(conversationId);

        if (conversation is null)
        {
            throw new KeyNotFoundException(
                "Söhbət tapılmadı.");
        }

        var isUser =
            conversation.UserId == userId;

        var isMechanic =
            conversation.Mechanic is not null &&
            conversation.Mechanic.UserId == userId;

        var isStore =
            conversation.Store is not null &&
            conversation.Store.UserId == userId;

        if (!isUser &&
            !isMechanic &&
            !isStore)
        {
            throw new UnauthorizedAccessException(
                "Bu söhbətə giriş icazən yoxdur.");
        }

        return conversation;
    }
}