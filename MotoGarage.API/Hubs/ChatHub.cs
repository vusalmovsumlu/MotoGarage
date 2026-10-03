using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MotoGarage.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(
        string conversationId,
        string message)
    {
        await Clients.Group(conversationId)
            .SendAsync(
                "ReceiveMessage",
                Context.ConnectionId,
                message
            );
    }

    public async Task JoinConversation(
        string conversationId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            conversationId
        );
    }

    public async Task LeaveConversation(
        string conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            conversationId
        );
    }
}