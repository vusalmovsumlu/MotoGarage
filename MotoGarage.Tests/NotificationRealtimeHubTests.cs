using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotoGarage.API.HostedServices;
using MotoGarage.API.Hubs;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Notifications;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class NotificationRealtimeHubTests
{
    [Theory]
    [InlineData(UserRole.Mechanic)]
    [InlineData(UserRole.Store)]
    public async Task CommittedNotificationReachesAllRecipientsConnectionsButNeverOtherAccounts(UserRole role)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        var services = builder.Services;
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddSignalR();
        services.AddSingleton<IUserConnectionRegistry, UserConnectionRegistry>();
        services.AddSingleton<NotificationChangeQueue>();
        services.AddSingleton<INotificationChangeQueue>(sp => sp.GetRequiredService<NotificationChangeQueue>());
        services.AddScoped<NotificationChangeInterceptor>();
        services.AddScoped<NotificationTransactionInterceptor>();
        services.AddHostedService<NotificationRealtimeWorker>();
        var database = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>((sp, options) => options.UseInMemoryDatabase(database)
            .AddInterceptors(sp.GetRequiredService<NotificationChangeInterceptor>(), sp.GetRequiredService<NotificationTransactionInterceptor>()));
        services.AddScoped<UserManager<User>>(sp => TestSupport.Users(sp.GetRequiredService<AppDbContext>()));
        services.AddScoped<IChatService>(sp =>
        {
            var c = sp.GetRequiredService<AppDbContext>();
            return new ChatService(new ConversationRepository(c), new MessageRepository(c), new MechanicRepository(c),
                new StoreRepository(c), TestSupport.Mapper(), new TestUnitOfWork(c),
                new NotificationService(new NotificationRepository(c), TestSupport.Mapper()));
        });
        await using var app = builder.Build();
        // Only this isolated test host accepts these headers. Production uses JWT.
        app.Use(async (context, next) =>
        {
            var id = context.Request.Headers["X-Test-User"].ToString();
            context.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Role, id == "2" ? role.ToString() : "User")], "IsolatedTest"));
            await next();
        });
        app.UseAuthorization();
        app.MapHub<ChatHub>("/hubs/chat");
        using (var scope = app.Services.CreateScope())
        {
            var c = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            c.Users.AddRange(new User { Id = 1, FullName = "Customer", EmailConfirmed = true },
                new User { Id = 2, FullName = "Business", EmailConfirmed = true, Role = role });
            if (role == UserRole.Store)
                c.Stores.Add(new Store { Id = 1, UserId = 2, StoreName = "Store", Slug = "store", Address = "Baku", Description = "Parts", PhoneNumber = "123", IsActive = true });
            else c.Mechanics.Add(new Mechanic { Id = 1, UserId = 2, BusinessName = "Garage", Slug = "garage", Address = "Baku", Description = "Service", PhoneNumber = "123", IsActive = true });
            c.Conversations.Add(new Conversation { Id = 10, UserId = 1, StoreId = role == UserRole.Store ? 1 : null, MechanicId = role == UserRole.Mechanic ? 1 : null });
            await c.SaveChangesAsync();
        }
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var uri = new Uri(address.Replace("http://", "ws://") + "/hubs/chat");
            using var customer = await Connect(uri, "1");
            using var business = await Connect(uri, "2");
            using var secondTab = await Connect(uri, "2");
            using (var scope = app.Services.CreateScope())
            {
                var c = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await new NotificationService(new NotificationRepository(c), TestSupport.Mapper())
                    .CreateAsync(2, NotificationType.Message, "New message", "Message", "/chat/10");
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            foreach (var socket in new[] { business, secondTab })
            {
                var frame = await Read(socket, timeout.Token);
                Assert.Contains("NotificationsChanged", frame);
                Assert.DoesNotContain("Message", frame); // The event is a hint, not private content.
            }
            using var noEvent = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Read(customer, noEvent.Token));
        }
        finally { await app.StopAsync(); }
    }

    static async Task<ClientWebSocket> Connect(Uri uri, string id)
    {
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("X-Test-User", id);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await socket.ConnectAsync(uri, timeout.Token);
        await Send(socket, "{\"protocol\":\"json\",\"version\":1}\u001e", timeout.Token);
        Assert.Equal("{}\u001e", await Read(socket, timeout.Token));
        // Await an authorized invocation to ensure OnConnected and user-group
        // registration have completed before generating the notification.
        await Send(socket, "{\"type\":1,\"invocationId\":\"ready\",\"target\":\"JoinConversation\",\"arguments\":[10]}\u001e", timeout.Token);
        var ready = await Read(socket, timeout.Token);
        Assert.Contains("ready", ready);
        Assert.DoesNotContain("error", ready);
        return socket;
    }

    static Task Send(ClientWebSocket socket, string value, CancellationToken token) =>
        socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(value)), WebSocketMessageType.Text, true, token);
    static async Task<string> Read(ClientWebSocket socket, CancellationToken token)
    {
        var data = new byte[4096];
        var result = new StringBuilder();
        WebSocketReceiveResult part;
        do
        {
            part = await socket.ReceiveAsync(new ArraySegment<byte>(data), token);
            if (part.MessageType == WebSocketMessageType.Close) throw new InvalidOperationException("Socket closed before the expected message.");
            result.Append(Encoding.UTF8.GetString(data, 0, part.Count));
        } while (!part.EndOfMessage);
        return result.ToString();
    }
}
