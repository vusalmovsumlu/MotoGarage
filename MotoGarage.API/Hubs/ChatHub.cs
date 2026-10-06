using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(
        IChatService chatService)
    {
        _chatService = chatService;
    }

    // =========================
    // Join conversation
    // =========================

    public async Task JoinConversation(
        int conversationId)
    {
        var userId = GetCurrentUserId();

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
        var userId = GetCurrentUserId();

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
        var userId = GetCurrentUserId();

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