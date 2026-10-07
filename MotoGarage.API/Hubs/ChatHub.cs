using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MotoGarage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MotoGarage.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUserConnectionRegistry _connections;
    private readonly IChatService _chatService;
    private readonly UserManager<User> _userManager;

    public ChatHub(
        IChatService chatService,
        UserManager<User> userManager,
        IUserConnectionRegistry connections)
    {
        _connections = connections;
        _chatService = chatService;
        _userManager = userManager;
    }

    // =========================
    // Join conversation
    // =========================

    public async Task JoinConversation(
        int conversationId)
    {
        var userId = await GetAuthorizedUserIdAsync();

        // İcazəsi olmayan user qrupa girə bilməz
        await _chatService.GetByIdAsync(
            conversationId,
            userId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationGroup(conversationId));
    }

    // =========================
    // Leave conversation
    // =========================

    public async Task LeaveConversation(
        int conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationGroup(conversationId));
    }

    // =========================
    // Send message
    // =========================

    public async Task SendMessage(
        int conversationId,
        string content)
    {
        var userId = await GetAuthorizedUserIdAsync();

        var dto = new SendMessageDto
        {
            Content = content
        };

        // Əvvəl DB-yə yazılır
        var message =
            await _chatService.SendMessageAsync(
                conversationId,
                userId,
                dto);

        // Sonra real-time qrupa göndərilir
        await Clients
            .Group(GetConversationGroup(conversationId))
            .SendAsync(
                "ReceiveMessage",
                message);
    }

    // =========================
    // Mark messages as read
    // =========================

    public async Task MarkAsRead(
        int conversationId)
    {
        var userId = await GetAuthorizedUserIdAsync();

        await _chatService.MarkAsReadAsync(
            conversationId,
            userId);

        await Clients
            .Group(GetConversationGroup(conversationId))
            .SendAsync(
                "MessagesRead",
                conversationId,
                userId);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await GetAuthorizedUserIdAsync();
        _connections.Register(userId, Context.ConnectionId, Context.Abort);
        try
        {
            // Close the race between authentication and registration after an admin block.
            await GetAuthorizedUserIdAsync();
            await base.OnConnectedAsync();
        }
        catch
        {
            _connections.Remove(userId, Context.ConnectionId);
            throw;
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connections.Remove(GetCurrentUserId(), Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<int> GetAuthorizedUserIdAsync()
    {
        var id = GetCurrentUserId();
        var user = await _userManager.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user is null || !user.EmailConfirmed || user.IsAdminBlocked ||
            (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow) ||
            Context.User?.FindFirstValue(ClaimTypes.Role) != user.Role.ToString())
        {
            Context.Abort();
            throw new HubException("Bu bağlantı üçün giriş icazəsi ləğv edilib.");
        }
        return id;
    }

    private int GetCurrentUserId()
    {
        var userId =
            Context.User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(userId, out var id))
        {
            throw new HubException(
                "İstifadəçi məlumatı tapılmadı.");
        }

        return id;
    }

    private static string GetConversationGroup(
        int conversationId)
    {
        return $"conversation-{conversationId}";
    }
}
