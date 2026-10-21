using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using static MotoGarage.Tests.ReminderEmailTests;
using Clock = MotoGarage.Tests.AppointmentEmailReminderTests.ReminderClock;

namespace MotoGarage.Tests;

public class ReminderEmailSqlTests
{
    [LocalReminderSqlFact]
    public async Task ConcurrentWorkersAndRejectedInsertsKeepNotificationAndEmailClaimsAtomic()
    {
        await InDatabase(async (context, options) =>
        {
            var reminder = await Seed(context);
            async Task Reset()
            {
                await context.WorkflowEmails.ExecuteDeleteAsync();
                await context.Notifications.ExecuteDeleteAsync();
                context.ChangeTracker.Clear();
                var record = await context.Reminders.SingleAsync();
                record.NotificationSentAtUtc = record.EmailQueuedAtUtc = null;
                record.IsCompleted = false;
                record.ReminderDate = Now;
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
            async Task AssertClaims(int count)
            {
                Assert.Equal(count, await context.WorkflowEmails.CountAsync());
                Assert.Equal(count, await context.Notifications.CountAsync());
                var record = await context.Reminders.AsNoTracking().SingleAsync();
                Assert.Equal(count == 1, record.EmailQueuedAtUtc.HasValue);
                Assert.Equal(count == 1, record.NotificationSentAtUtc.HasValue);
            }
            var race = new SaveRace();
            var racing = new DbContextOptionsBuilder<AppDbContext>(options).AddInterceptors(race).Options;
            await using (var first = new AppDbContext(racing))
            await using (var second = new AppDbContext(racing))
                Assert.Equal(1, (await Task.WhenAll(Dispatcher(first).DispatchDueAsync(), Dispatcher(second).DispatchDueAsync())).Sum());
            await AssertClaims(1);
            Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());

            // A notification or email insert failure must roll back both claims and both inserts.
            foreach (var table in new[] { "Notifications", "WorkflowEmails" })
            {
                await Reset();
                var create = table == "Notifications"
                    ? "ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Test_ReminderEmailFailure] CHECK ([UserId] < 0);"
                    : "ALTER TABLE [WorkflowEmails] ADD CONSTRAINT [CK_Test_ReminderEmailFailure] CHECK ([RecipientUserId] < 0);";
                var drop = table == "Notifications"
                    ? "ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Test_ReminderEmailFailure];"
                    : "ALTER TABLE [WorkflowEmails] DROP CONSTRAINT [CK_Test_ReminderEmailFailure];";
                await context.Database.ExecuteSqlRawAsync(create);
                await using (var failing = new AppDbContext(options))
                    await Assert.ThrowsAsync<ConflictException>(() => Dispatcher(failing).DispatchDueAsync());
                await AssertClaims(0);
                await context.Database.ExecuteSqlRawAsync(drop);
                Assert.Equal(1, await Dispatcher(context).DispatchDueAsync());
                await AssertClaims(1);
            }

            // Concurrent owner edits between the due query and SaveChanges invalidate the whole batch.
            foreach (var complete in new[] { false, true })
            {
                await Reset();
                var pause = new PausedSave();
                await using var sender = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>(options).AddInterceptors(pause).Options);
                var dispatch = Dispatcher(sender).DispatchDueAsync();
                await pause.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using (var editor = new AppDbContext(options))
                {
                    if (complete) await Service(editor).MarkAsCompletedAsync(reminder.Id, reminder.Vehicle.UserId);
                    else await Service(editor).UpdateAsync(reminder.Id, reminder.Vehicle.UserId,
                        new() { Title = "Baxış", ReminderDate = Now.AddDays(1) });
                }
                pause.Release.TrySetResult();
                Assert.Equal(0, await dispatch);
                await AssertClaims(0);
            }
        });
    }

    [LocalReminderSqlFact]
    public async Task SmtpRetriesSurviveRestartAndObsoleteRemindersAreDiscardedBeforeSending()
    {
        await InDatabase(async (context, options) =>
        {
            var reminder = await Seed(context);
            var clock = new Clock(Now);
            var mail = new WorkflowEmailTests.Mail { Fail = true };
            Assert.Equal(1, await Dispatcher(context, clock).DispatchDueAsync());
            Assert.Equal(0, await WorkflowEmailSqlTests.Dispatcher(context, mail, clock).DispatchDueAsync());
            Assert.Equal(1, mail.Attempts);
            Assert.Equal(0, await WorkflowEmailSqlTests.Dispatcher(context, mail, clock).DispatchDueAsync());
            Assert.Equal(1, mail.Attempts);
            var queued = await context.WorkflowEmails.AsNoTracking().SingleAsync();
            Assert.NotNull(queued.NextAttemptAtUtc);
            Assert.Null(queued.SentAtUtc);
            mail.Fail = false;
            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            await using (var restarted = new AppDbContext(options))
            {
                Assert.Equal(0, await Dispatcher(restarted, clock).DispatchDueAsync());
                Assert.Equal(1, await WorkflowEmailSqlTests.Dispatcher(restarted, mail, clock).DispatchDueAsync());
            }
            Assert.Equal(queued.DeliveryId, Assert.Single(mail.Sent).Content.DeliveryId);
            Assert.Equal("reminder@example.invalid", Assert.Single(mail.Sent).Recipient);
            Assert.Single(await context.WorkflowEmails.ToListAsync());

            foreach (var action in new[] { "complete", "delete", "reschedule", "unverified", "blocked", "deleted", "odometer" })
            {
                await context.WorkflowEmails.ExecuteDeleteAsync();
                await context.Notifications.ExecuteDeleteAsync();
                await context.Reminders.ExecuteDeleteAsync();
                context.ChangeTracker.Clear();
                var record = new Reminder
                {
                    VehicleId = reminder.VehicleId, Title = "Baxış",
                    ReminderDate = action == "odometer" ? Now.AddDays(1) : Now,
                    ReminderKm = action == "odometer" ? 25000 : null
                };
                var owner = await context.Users.SingleAsync();
                owner.EmailConfirmed = true; owner.IsAdminBlocked = false; owner.DeletedAtUtc = null;
                context.Reminders.Add(record);
                await context.SaveChangesAsync();
                Assert.Equal(1, await Dispatcher(context, clock).DispatchDueAsync());
                var service = Service(context);
                if (action == "complete") await service.MarkAsCompletedAsync(record.Id, owner.Id);
                if (action == "delete") await service.DeleteAsync(record.Id, owner.Id);
                if (action == "reschedule") await service.UpdateAsync(record.Id, owner.Id, new() { Title = "Baxış", ReminderDate = Now.AddDays(1) });
                if (action == "unverified" || action == "blocked" || action == "deleted")
                {
                    owner = await context.Users.SingleAsync();
                    if (action == "unverified") owner.EmailConfirmed = false;
                    if (action == "blocked") owner.IsAdminBlocked = true;
                    if (action == "deleted") owner.DeletedAtUtc = Now;
                    await context.SaveChangesAsync();
                }
                if (action == "odometer")
                {
                    (await context.Vehicles.SingleAsync()).CurrentKm = 24000;
                    await context.SaveChangesAsync();
                }
                var attempts = mail.Attempts;
                Assert.Equal(0, await WorkflowEmailSqlTests.Dispatcher(context, mail, clock).DispatchDueAsync());
                Assert.Equal(attempts, mail.Attempts);
                Assert.NotNull((await context.WorkflowEmails.AsNoTracking().SingleAsync()).DiscardedAtUtc);
            }
        });
    }

    private static async Task InDatabase(Func<AppDbContext, DbContextOptions<AppDbContext>, Task> verify)
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use the isolated LocalDB test configuration.");
        var name = "MotoGarage_ReminderEmailChecks_" + Guid.NewGuid().ToString("N");
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
            var target = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
            if (target.DataSource != connection.DataSource || target.InitialCatalog != name || !name.StartsWith("MotoGarage_ReminderEmailChecks_"))
                throw new InvalidOperationException("Unsafe test database cleanup target.");
            await context.Database.EnsureDeletedAsync();
        }
    }

    sealed class SaveRace : SaveChangesInterceptor
    {
        int arrivals;
        readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) <= 2)
            {
                if (Volatile.Read(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }

    sealed class PausedSave : SaveChangesInterceptor
    {
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Ready.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return result;
        }
    }
}
