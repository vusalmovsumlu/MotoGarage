using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Inventory;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Storage;

namespace MotoGarage.Tests;

public class OrderDeliveryChatMediaSqlTests
{
    [LocalReminderSqlFact]
    public async Task MigrationKeepsLegacyHistoryAndConcurrentAttachmentsAndFailedSendAreAtomic()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use isolated LocalDB test configuration.");
        var name = "MotoGarage_DeliveryMediaChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        DbContextOptions<AppDbContext> Options(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString,
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(300), [1205]));
            if (interceptor != null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }
        await using var c = new AppDbContext(Options());
        var storage = new OrderDeliveryAndChatMediaTests.FakeMedia();
        ChatAttachmentService Media(AppDbContext db) => new(db, new UnitOfWork(db), new ConversationRepository(db), new AccountAccessService(db), storage, NullLogger<ChatAttachmentService>.Instance);
        ChatService Chat(AppDbContext db) => new(new ConversationRepository(db), new MessageRepository(db), new MechanicRepository(db), new StoreRepository(db), TestSupport.Mapper(), new UnitOfWork(db), new NotificationService(new NotificationRepository(db), TestSupport.Mapper()), Media(db));
        try
        {
            var previous = c.Database.GetMigrations().TakeWhile(x => !x.EndsWith("OrderDeliveryAndChatMedia")).Last();
            await c.GetService<IMigrator>().MigrateAsync(previous);
            await c.Database.ExecuteSqlRawAsync("""
                INSERT INTO [AspNetUsers] ([FullName], [Role], [CreatedAt], [EmailConfirmed], [IsAdminBlocked], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [BusinessApprovalStatus])
                VALUES (N'Customer', 1, SYSUTCDATETIME(), 1, 0, 0, 0, 0, 0, 2), (N'Shop', 3, SYSUTCDATETIME(), 1, 0, 0, 0, 0, 0, 2);
                INSERT INTO [Stores] ([UserId], [StoreName], [Slug], [Description], [PhoneNumber], [Address], [Rating], [IsActive], [CreatedAt])
                VALUES (2, N'Shop', 'shop', N'Parts', '123', N'Baku', 0, 1, SYSUTCDATETIME());
                INSERT INTO [Conversations] ([UserId], [StoreId], [CreatedAt]) VALUES (1, 1, SYSUTCDATETIME());
                INSERT INTO [Messages] ([ConversationId], [SenderUserId], [Content], [SentAt], [Status]) VALUES (1, 1, N'Legacy message', SYSUTCDATETIME(), 1);
                INSERT INTO [Orders] ([UserId], [StoreId], [TotalAmount], [Status], [CreatedAt]) VALUES (1, 1, 10, 5, SYSUTCDATETIME());
                """);
            await c.Database.MigrateAsync();
            Assert.Null((await c.Orders.SingleAsync()).DeliveryAddress);
            Assert.Null((await c.Messages.SingleAsync()).AttachmentId);
            Assert.Equal("Legacy message", (await c.Messages.SingleAsync()).Content);
            var product = new Product { StoreId = 1, Name = "Product", Slug = "product", Price = 10, Stock = 5, IsActive = true };
            c.Products.Add(product); await c.SaveChangesAsync();
            var orderService = new OrderService(new OrderRepository(c), new ProductRepository(c), new StoreRepository(c), TestSupport.Mapper(), new UnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), new AccountAccessService(c), new StockAlertService(c));
            var order = await orderService.CreateAsync(1, TestSupport.Order(product.Id));
            Assert.Equal("Test küçəsi 10, mənzil 2", order.DeliveryAddress);

            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
            var file = await Media(c).UploadAsync(1, 1, new MemoryStream(png), "photo.png", "image/png");
            async Task<bool> SendOnce()
            {
                await using var attempt = new AppDbContext(Options());
                try { await Chat(attempt).SendMessageAsync(1, 1, new() { AttachmentId = file.Id }); return true; }
                catch (ConflictException) { return false; }
            }
            var results = await Task.WhenAll(SendOnce(), SendOnce());
            Assert.Single(results.Where(x => x));
            c.ChangeTracker.Clear();
            Assert.Equal(1, await c.Messages.CountAsync(x => x.AttachmentId == file.Id));
            Assert.Equal(1, await c.Notifications.CountAsync(x => x.Type == NotificationType.Message));
            Assert.Equal(png, (await Media(c).ReadAsync(file.Id, 1, 2)).Bytes);

            var pending = await Media(c).UploadAsync(1, 1, new MemoryStream(png), "next.png", "image/png");
            await using (var failed = new AppDbContext(Options(new FailNotification())))
                await Assert.ThrowsAsync<DomainException>(() => Chat(failed).SendMessageAsync(1, 1, new() { AttachmentId = pending.Id }));
            c.ChangeTracker.Clear();
            Assert.False((await c.ChatAttachments.SingleAsync(x => x.Id == pending.Id)).IsAttached);
            Assert.False(await c.Messages.AnyAsync(x => x.AttachmentId == pending.Id));
            await Chat(c).SendMessageAsync(1, 1, new() { Content = "Retry", AttachmentId = pending.Id });
            Assert.Equal(1, await c.Messages.CountAsync(x => x.AttachmentId == pending.Id));
        }
        finally
        {
            await c.Database.CloseConnectionAsync();
            if (connection.InitialCatalog != name || !name.StartsWith("MotoGarage_DeliveryMediaChecks_")) throw new InvalidOperationException("Unsafe cleanup.");
            await c.Database.EnsureDeletedAsync();
        }
    }

    sealed class FailNotification : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context!.ChangeTracker.Entries<Notification>().Any(x => x.State == EntityState.Added)) throw new DomainException("Test notification failure.");
            return ValueTask.FromResult(result);
        }
    }
}
