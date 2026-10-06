using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MotoGarage.API.Hubs;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(
        IChatService chatService,
        IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
    }

    // GET: api/chat/conversations
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        var userId = GetCurrentUserId();

        var conversations =
            await _chatService.GetMyConversationsAsync(
                userId);

        return Ok(conversations);
    }

    // GET: api/chat/conversations/5
    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(
        int id)
    {
        var userId = GetCurrentUserId();

        var conversation =
            await _chatService.GetByIdAsync(
                id,
                userId);

        return Ok(conversation);
    }

    // GET: api/chat/conversations/5/messages
    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(
        int id)
    {
        var userId = GetCurrentUserId();

        var messages =
            await _chatService.GetMessagesAsync(
                id,
                userId);

        return Ok(messages);
    }

    // POST: api/chat/mechanic/3
    [HttpPost("mechanic/{mechanicId}")]
    public async Task<IActionResult> StartMechanicConversation(
        int mechanicId)
    {
        var userId = GetCurrentUserId();

        var conversation =
            await _chatService.StartMechanicConversationAsync(
                userId,
                mechanicId);

        return Ok(conversation);
    }

    // POST: api/chat/store/4
    [HttpPost("store/{storeId}")]
    public async Task<IActionResult> StartStoreConversation(
        int storeId)
    {
        var userId = GetCurrentUserId();

        var conversation =
            await _chatService.StartStoreConversationAsync(
                userId,
                storeId);

        return Ok(conversation);
    }

    // POST: api/chat/conversations/5/messages
    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(
        int id,
        SendMessageDto dto)
    {
        var userId = GetCurrentUserId();

        var message =
            await _chatService.SendMessageAsync(
                id,
                userId,
                dto);

        await _hubContext.Clients
            .Group(GetConversationGroup(id))
            .SendAsync(
                "ReceiveMessage",
                message);

        return Ok(message);
    }

    // PATCH: api/chat/conversations/5/read
    [HttpPatch("conversations/{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id)
    {
        var userId = GetCurrentUserId();

        await _chatService.MarkAsReadAsync(
            id,
            userId);

        await _hubContext.Clients
            .Group(GetConversationGroup(id))
            .SendAsync(
                "MessagesRead",
                id,
                userId);

        return NoContent();
    }

    // GET: api/chat/conversations/5/unread-count
    [HttpGet("conversations/{id}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(
        int id)
    {
        var userId = GetCurrentUserId();

        var count =
            await _chatService.GetUnreadCountAsync(
                id,
                userId);

        return Ok(new
        {
            unreadCount = count
        });
    }

    private int GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
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