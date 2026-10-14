using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Email;
using static MotoGarage.Tests.WorkflowEmailTests;
using Clock = MotoGarage.Tests.AppointmentEmailReminderTests.ReminderClock;

namespace MotoGarage.Tests;

public class WorkflowEmailSqlTests
{
    internal static WorkflowEmailDispatcher Dispatcher(AppDbContext c, Mail mail, Clock clock, WorkflowEmailOptions? options = null) =>
        new(c, mail, clock, Options.Create(options ?? new WorkflowEmailOptions()), NullLogger<WorkflowEmailDispatcher>.Instance);

    [LocalReminderSqlFact]
    public async Task BusinessEventsCommitWithEmailSnapshotsAndRollbackLeavesNoEmailOrStockChange()
    {
        await InDatabase(async (c, options) =>
        {
            await Seed(c);
            var mail = new Mail { Fail = true };
            var orders = Orders(c, new UnitOfWork(c));
            await c.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] WITH NOCHECK ADD CONSTRAINT [CK_Test_WorkflowEmail] CHECK ([Title] <> N'Yeni sifariş');");
            await Assert.ThrowsAsync<ConflictException>(() => orders.CreateAsync(1, TestSupport.Order()));
            c.ChangeTracker.Clear();
            Assert.Empty(c.Orders);
            Assert.Empty(c.WorkflowEmails);
            Assert.Empty(c.Notifications);
            Assert.Equal(10, (await c.Products.SingleAsync()).Stock);
            await c.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Test_WorkflowEmail];");
            var order = await orders.CreateAsync(1, TestSupport.Order());
            foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Completed })
                await orders.UpdateStatusAsync(order.Id, 3, new() { Status = status });
            Assert.Equal(0, mail.Attempts); // No SMTP in the request transaction, even when SMTP is unavailable.
            Assert.Equal(5, await c.WorkflowEmails.CountAsync());
            var clock = new Clock(DateTime.UtcNow);
            Assert.Equal(0, await Dispatcher(c, mail, clock).DispatchDueAsync());
            c.ChangeTracker.Clear();
            var pending = await c.WorkflowEmails.OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(1, pending[0].Attempts);
            Assert.Equal(clock.UtcNow.AddSeconds(60), pending[0].NextAttemptAtUtc);
            Assert.All(pending.Skip(1), x => Assert.Equal(0, x.Attempts));
            Assert.Equal(OrderStatus.Completed, (await c.Orders.SingleAsync()).Status);
            var deliveryId = pending[0].DeliveryId;
            (await c.Users.FindAsync(1))!.Email = "new-address@example.invalid";
            await c.SaveChangesAsync();
            mail.Fail = false; clock.UtcNow = clock.UtcNow.AddSeconds(60);
            await using (var restarted = new AppDbContext(options))
                Assert.Equal(5, await Dispatcher(restarted, mail, clock).DispatchDueAsync());
            Assert.Equal(deliveryId, mail.Sent.First().Content.DeliveryId);
            Assert.All(mail.Sent, x => Assert.Equal("new-address@example.invalid", x.Recipient));
            Assert.Equal(pending.Select(x => x.DeliveryId), mail.Sent.Select(x => x.Content.DeliveryId));
            Assert.Equal(0, await Dispatcher(c, mail, clock).DispatchDueAsync());

            var appointment = await Appointments(c, new UnitOfWork(c)).CreateAsync(1,
                new() { MechanicId = 1, VehicleId = 1, AppointmentDate = BusinessOperationsTests.Slot() });
            await Appointments(c, new UnitOfWork(c)).UpdateStatusAsync(appointment.Id, 2, new() { Status = AppointmentStatus.Accepted });
            Assert.Equal(2, await Dispatcher(c, mail, clock).DispatchDueAsync());
            Assert.Contains("Görüşünüz usta tərəfindən qəbul olundu", mail.Sent.Last().Content.TextBody);
            Assert.Equal(7, mail.Sent.Count);
        });
    }

    [LocalReminderSqlFact]
    public async Task ConcurrentWorkersExpiredLeasesDiscardedAccountsAndBoundedRetriesUseRealSql()
    {
        await InDatabase(async (c, options) =>
        {
            await Seed(c);
            var clock = new Clock(DateTime.UtcNow);
            WorkflowEmail Message(string key) => new()
            {
                EventKey = key, RecipientUserId = 1, Subject = key, TextBody = key, HtmlBody = key, CreatedAtUtc = clock.UtcNow
            };
            c.WorkflowEmails.AddRange(Message("first"), Message("second"));
            await c.SaveChangesAsync();
            var mail = new Mail();
            var barrier = new CandidateBarrier();
            var racingOptions = new DbContextOptionsBuilder<AppDbContext>(options).AddInterceptors(barrier).Options;
            await using var first = new AppDbContext(racingOptions);
            await using var second = new AppDbContext(racingOptions);
            var counts = await Task.WhenAll(Dispatcher(first, mail, clock).DispatchDueAsync(), Dispatcher(second, mail, clock).DispatchDueAsync());
            Assert.Equal(2, counts.Sum());
            Assert.Equal(new[] { "first", "second" }, mail.Sent.Select(x => x.Content.Subject));
            Assert.Equal(2, mail.Sent.Select(x => x.Content.DeliveryId).Distinct().Count());
            Assert.Equal(0, await Dispatcher(c, mail, clock).DispatchDueAsync());

            var recovery = Message("recovery");
            recovery.LeaseId = Guid.NewGuid(); recovery.LeaseUntilUtc = clock.UtcNow.AddSeconds(10); recovery.Attempts = 1;
            c.WorkflowEmails.Add(recovery); await c.SaveChangesAsync();
            Assert.Equal(0, await Dispatcher(c, mail, clock).DispatchDueAsync());
            clock.UtcNow = clock.UtcNow.AddSeconds(10);
            Assert.Equal(1, await Dispatcher(c, mail, clock).DispatchDueAsync());
            Assert.Equal("recovery", mail.Sent.Last().Content.Subject);

            var discard = Message("discard"); c.WorkflowEmails.Add(discard); await c.SaveChangesAsync();
            (await c.Users.FindAsync(1))!.IsAdminBlocked = true;
            await c.SaveChangesAsync();
            Assert.Equal(0, await Dispatcher(c, mail, clock).DispatchDueAsync());
            Assert.Equal(3, mail.Attempts);
            Assert.NotNull((await c.WorkflowEmails.AsNoTracking().SingleAsync(x => x.EventKey == "discard")).DiscardedAtUtc);
            c.ChangeTracker.Clear();
            (await c.Users.FindAsync(1))!.IsAdminBlocked = false;
            await c.SaveChangesAsync();

            c.WorkflowEmails.Add(Message("exhausted")); await c.SaveChangesAsync();
            mail.Fail = true;
            var deliveryOptions = new WorkflowEmailOptions { MaxAttempts = 2 };
            Assert.Equal(0, await Dispatcher(c, mail, clock, deliveryOptions).DispatchDueAsync());
            Assert.Equal(0, await Dispatcher(c, mail, clock, deliveryOptions).DispatchDueAsync());
            Assert.Equal(4, mail.Attempts);
            clock.UtcNow = clock.UtcNow.AddSeconds(60);
            Assert.Equal(0, await Dispatcher(c, mail, clock, deliveryOptions).DispatchDueAsync());
            var exhausted = await c.WorkflowEmails.AsNoTracking().SingleAsync(x => x.EventKey == "exhausted");
            Assert.NotNull(exhausted.FailedAtUtc); Assert.Null(exhausted.LeaseId);
            Assert.Equal(2, exhausted.Attempts);
            Assert.Equal(0, await Dispatcher(c, mail, clock, deliveryOptions).DispatchDueAsync());
            Assert.Equal(5, mail.Attempts);
            c.WorkflowEmails.Add(Message("after-failed")); await c.SaveChangesAsync();
            mail.Fail = false;
            Assert.Equal(0, await Dispatcher(c, mail, clock, new() { Enabled = false }).DispatchDueAsync());
            Assert.Equal(1, await Dispatcher(c, mail, clock).DispatchDueAsync());
            Assert.Equal("after-failed", mail.Sent.Last().Content.Subject);
        });
    }

    private static async Task InDatabase(Func<AppDbContext, DbContextOptions<AppDbContext>, Task> verify)
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use the isolated LocalDB test configuration.");
        var name = "MotoGarage_WorkflowEmailChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString,
            sql => sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(200), [1205])).Options;
        await using var context = new AppDbContext(options);
        try
        {
            await context.Database.MigrateAsync();
            await verify(context, options);
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (connection.InitialCatalog != name || !name.StartsWith("MotoGarage_WorkflowEmailChecks_"))
                throw new InvalidOperationException("Unsafe test cleanup target.");
            await context.Database.EnsureDeletedAsync();
        }
    }

    sealed class CandidateBarrier : DbCommandInterceptor
    {
        int arrivals;
        readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("SELECT TOP") && command.CommandText.Contains("[WorkflowEmails]") &&
                !command.CommandText.Contains("[DeliveryId]") && Interlocked.Increment(ref arrivals) <= 2)
            {
                if (Volatile.Read(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
