using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class MessageRepository
    : Repository<Message>, IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(
        int conversationId)
    {
        return await _context.Messages
            .Include(x => x.SenderUser)
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(
        int conversationId,
        int userId)
    {
        return await _context.Messages
            .Where(x =>
                x.ConversationId == conversationId &&
                x.SenderUserId != userId &&
                x.Status != MessageStatus.Read)
            .OrderBy(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(
        int conversationId,
        int userId)
    {
        return await _context.Messages
            .CountAsync(x =>
                x.ConversationId == conversationId &&
                x.SenderUserId != userId &&
                x.Status != MessageStatus.Read);
    }
}