using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class NotificationDeletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnerCanDeleteReadOrUnreadNotification(bool read)
    {
        using var context = TestSupport.Context();
        context.Notifications.AddRange(
            new Notification { Id = 1, UserId = 1, Title = "Own", Message = "Own", IsRead = read },
            new Notification { Id = 2, UserId = 2, Title = "Other", Message = "Other" });
        await context.SaveChangesAsync();
        var service = new NotificationService(new NotificationRepository(context), TestSupport.Mapper());
        await service.DeleteAsync(1, 1);
        Assert.Equal(2, Assert.Single(context.Notifications).Id);
        Assert.Equal(0, await service.GetUnreadCountAsync(1));
        Assert.Equal(1, await service.GetUnreadCountAsync(2));
    }

    [Fact]
    public async Task DeletingAnotherOwnersOrMissingNotificationDoesNotChangeData()
    {
        using var context = TestSupport.Context();
        context.Notifications.Add(new Notification { Id = 1, UserId = 1, Title = "Own", Message = "Own" });
        await context.SaveChangesAsync();
        var service = new NotificationService(new NotificationRepository(context), TestSupport.Mapper());
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1, 2));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(99, 1));
        Assert.Single(context.Notifications);
        Assert.Equal(1, await service.GetUnreadCountAsync(1));
    }

    [LocalReminderSqlFact]
    public async Task BulkDeleteOnlyRemovesCurrentOwnersReadAndUnreadNotifications()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_NotificationChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        if (new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog != databaseName)
            throw new InvalidOperationException("Unexpected test database.");
        try
        {
            await context.Database.EnsureCreatedAsync();
            var owner = new User { FullName = "Owner" };
            var other = new User { FullName = "Other" };
            context.Users.AddRange(owner, other);
            await context.SaveChangesAsync();
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, Title = "Unread", Message = "Unread" },
                new Notification { UserId = owner.Id, Title = "Read", Message = "Read", IsRead = true },
                new Notification { UserId = other.Id, Title = "Other", Message = "Other" });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var service = new NotificationService(new NotificationRepository(context), TestSupport.Mapper());
            await service.DeleteAllAsync(owner.Id);
            Assert.Empty(await service.GetMyNotificationsAsync(owner.Id, false));
            Assert.Equal(0, await service.GetUnreadCountAsync(owner.Id));
            Assert.Equal(other.Id, (await context.Notifications.SingleAsync()).UserId);
            Assert.Equal(1, await service.GetUnreadCountAsync(other.Id));
            await service.DeleteAllAsync(owner.Id);
            Assert.Single(context.Notifications);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
