using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Notifications;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class NotificationRealtimeSqlTests
{
    [LocalSqlFact]
    public async Task LiveHintsWaitForCommitAndNeverPublishRolledBackOrFailedNotifications()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use the isolated LocalDB test configuration.");
        var name = "MotoGarage_RealtimeChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        var queue = new Hints();
        var changes = new NotificationChangeInterceptor(queue);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString)
            .AddInterceptors(changes, new NotificationTransactionInterceptor(changes)).Options;
        await using var c = new AppDbContext(options);
        try
        {
            await c.Database.MigrateAsync();
            c.Users.Add(new User { FullName = "Recipient", EmailConfirmed = true });
            await c.SaveChangesAsync();
            var id = c.Users.Single().Id;
            Assert.Empty(queue.Users);
            await using (var transaction = await c.Database.BeginTransactionAsync())
            {
                c.Notifications.Add(Notification(id));
                await c.SaveChangesAsync();
                Assert.Empty(queue.Users);
                await transaction.CommitAsync();
            }
            Assert.Equal([id], queue.Users);
            queue.Users.Clear();
            await using (var transaction = await c.Database.BeginTransactionAsync())
            {
                c.Notifications.Add(Notification(id));
                await c.SaveChangesAsync();
                Assert.Empty(queue.Users);
                await transaction.RollbackAsync();
            }
            Assert.Empty(queue.Users);
            c.ChangeTracker.Clear();
            Assert.Equal(1, await c.Notifications.CountAsync());
            // Two inserts exercise EF's implicit SaveChanges transaction too.
            c.Notifications.AddRange(Notification(id), Notification(id));
            await c.SaveChangesAsync();
            Assert.Equal([id], queue.Users);
            queue.Users.Clear();
            var existing = await c.Notifications.FirstAsync();
            existing.IsRead = true;
            c.SaveChanges(); // Sync paths used by integrations must also be covered.
            Assert.Equal([id], queue.Users);
            queue.Users.Clear();
            c.Notifications.Add(Notification(int.MaxValue));
            await Assert.ThrowsAsync<ConflictException>(() => c.SaveChangesAsync());
            Assert.Empty(queue.Users);
            c.ChangeTracker.Clear();
            await new NotificationRepository(c, changes).DeleteByUserIdAsync(id);
            Assert.Equal([id], queue.Users);
            Assert.Empty(await c.Notifications.ToListAsync());
        }
        finally
        {
            await c.Database.CloseConnectionAsync();
            if (connection.InitialCatalog != name || !name.StartsWith("MotoGarage_RealtimeChecks_")) throw new InvalidOperationException("Unsafe cleanup.");
            await c.Database.EnsureDeletedAsync();
        }
    }

    static Notification Notification(int id) => new() { UserId = id, Title = "Test", Message = "Test", CreatedAt = DateTime.UtcNow };
    sealed class Hints : INotificationChangeQueue
    {
        public List<int> Users { get; } = [];
        public void Enqueue(IEnumerable<int> userIds) => Users.AddRange(userIds);
    }
}
