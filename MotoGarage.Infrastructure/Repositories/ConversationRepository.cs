using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class ConversationRepository
    : Repository<Conversation>, IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Conversation>> GetByUserIdAsync(int userId)
    {
        return await _context.Conversations.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Mechanic)
            .Include(x => x.Store)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Conversation>> GetByMechanicIdAsync(
        int mechanicId)
    {
        return await _context.Conversations.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Mechanic)
            .Include(x => x.Store)
            .Where(x => x.MechanicId == mechanicId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Conversation>> GetByStoreIdAsync(
        int storeId)
    {
        return await _context.Conversations.AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Mechanic)
            .Include(x => x.Store)
            .Where(x => x.StoreId == storeId)
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToListAsync();
    }

    public Task<Conversation?> GetParticipantsAsync(int conversationId) =>
        _context.Conversations.Include(x => x.User).Include(x => x.Mechanic).Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == conversationId);

    public async Task<Conversation?> GetWithMessagesAsync(
        int conversationId)
    {
        return await _context.Conversations
            .Include(x => x.User)
            .Include(x => x.Mechanic)
            .Include(x => x.Store)
            .Include(x => x.Messages)
                .ThenInclude(x => x.SenderUser)
            .FirstOrDefaultAsync(x => x.Id == conversationId);
    }

    public async Task<Conversation?> GetUserMechanicConversationAsync(
        int userId,
        int mechanicId)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.MechanicId == mechanicId);
    }

    public async Task<Conversation?> GetUserStoreConversationAsync(
        int userId,
        int storeId)
    {
        return await _context.Conversations
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.StoreId == storeId);
    }
}
