using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Notifications;

namespace MotoGarage.Tests;

// Explicit opt-in: these checks create and remove a randomly named, isolated local SQL database.
public class ReminderNotificationSqlTests
{
    [LocalReminderSqlFact]
    public async Task SqlTransactionsPreventDuplicateStaleAndPartialDelivery()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var testDatabase = "MotoGarage_ReminderChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = testDatabase;
        DbContextOptions<AppDbContext> DbOptions(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString,
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), [1205]));
            if (interceptor != null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }
        var databaseOptions = DbOptions();
        await using var database = new AppDbContext(databaseOptions);
        if (new SqlConnectionStringBuilder(database.Database.GetConnectionString()).InitialCatalog != testDatabase || !testDatabase.StartsWith("MotoGarage_ReminderChecks_"))
            throw new InvalidOperationException("Refusing to use the application database for integration checks.");
        try
        {
            await database.Database.EnsureCreatedAsync();
            var owner = new User { FullName = "Reminder Integration", EmailConfirmed = true };
            var vehicle = new Vehicle
            {
                User = owner,
                Brand = "Toyota",
                Model = "Corolla",
                VIN = "SQL-TEST-VIN",
                PlateNumber = "SQL-TEST",
                Engine = "1.8",
                Year = 2022,
                CurrentKm = 25000,
                VehicleType = VehicleType.Car,
                FuelType = FuelType.Petrol
            };
            database.Vehicles.Add(vehicle);
            await database.SaveChangesAsync();
            async Task<int> Reset()
            {
                await database.Notifications.ExecuteDeleteAsync();
                await database.Reminders.ExecuteDeleteAsync();
                database.ChangeTracker.Clear();
                var reminder = new Reminder { VehicleId = vehicle.Id, Title = "Yağ dəyişimi", ReminderDate = DateTime.UtcNow.AddDays(-1) };
                database.Reminders.Add(reminder);
                await database.SaveChangesAsync();
                database.ChangeTracker.Clear();
                return reminder.Id;
            }
            ReminderNotificationDispatcher Dispatcher(AppDbContext context) => new(context, TimeProvider.System, Options.Create(new ReminderNotificationOptions()));
            async Task AssertState(int reminderId, int notifications, bool claimed)
            {
                database.ChangeTracker.Clear();
                Check(await database.Notifications.CountAsync() == notifications, "Unexpected notification count");
                Check((await database.Reminders.SingleAsync(x => x.Id == reminderId)).NotificationSentAtUtc.HasValue == claimed, "Unexpected delivery claim");
            }
            var id = await Reset();
            var race = new SaveRaceBarrier();
            await using (var first = new AppDbContext(DbOptions(race)))
            await using (var second = new AppDbContext(DbOptions(race)))
            {
                var results = await Task.WhenAll(Dispatcher(first).DispatchDueAsync(), Dispatcher(second).DispatchDueAsync());
                Check(results.Sum() == 1, "Two concurrent workers delivered more than once");
            }
            await AssertState(id, 1, true);
            Console.WriteLine("PASS: two concurrent workers commit exactly one notification.");

            foreach (var complete in new[] { false, true })
            {
                id = await Reset();
                var paused = new PausedSave();
                await using var sender = new AppDbContext(DbOptions(paused));
                var dispatch = Dispatcher(sender).DispatchDueAsync();
                await paused.Ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using (var editor = new AppDbContext(databaseOptions))
                {
                    var reminder = await editor.Reminders.SingleAsync(x => x.Id == id);
                    if (complete) reminder.IsCompleted = true;
                    else reminder.ReminderDate = DateTime.UtcNow.AddDays(1);
                    await editor.SaveChangesAsync();
                }
                paused.Release.TrySetResult();
                Check(await dispatch == 0, "A stale reminder was delivered");
                await AssertState(id, 0, false);
                Console.WriteLine(complete ? "PASS: concurrent completion cancels stale delivery." : "PASS: concurrent rescheduling cancels stale delivery.");
            }

            id = await Reset();
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Test_ReminderFailure] CHECK ([Type] <> 2);");
            await using (var failing = new AppDbContext(databaseOptions))
            {
                var rejected = false;
                try { await Dispatcher(failing).DispatchDueAsync(); }
                catch (ConflictException) { rejected = true; }
                Check(rejected, "Expected the database to reject the notification");
            }
            await AssertState(id, 0, false);
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Test_ReminderFailure];");
            await using (var retry = new AppDbContext(databaseOptions)) Check(await Dispatcher(retry).DispatchDueAsync() == 1, "Failed delivery could not be retried");
            await AssertState(id, 1, true);
            Console.WriteLine("PASS: failed insert rolls back the claim; retry delivers successfully.");

            await using (var restart = new AppDbContext(databaseOptions)) Check(await Dispatcher(restart).DispatchDueAsync() == 0, "Restart duplicated delivery");
            Console.WriteLine("PASS: delivery remains deduplicated after a new context/restart.");
            Check(await database.Notifications.AllAsync(x => x.UserId == owner.Id && x.Link == $"/garage/{vehicle.Slug}/reminders"), "Incorrect recipient/link");
            Console.WriteLine("PASS: notification belongs to the vehicle owner and links to that vehicle.");
        }
        finally
        {
            // Only this randomly named, isolated test database can be dropped.
            var target = new SqlConnectionStringBuilder(database.Database.GetConnectionString()).InitialCatalog;
            if (target != testDatabase || !target.StartsWith("MotoGarage_ReminderChecks_") || target == "MotoGarageDb")
                throw new InvalidOperationException("Unsafe integration database cleanup target.");
            await database.Database.EnsureDeletedAsync();
            Console.WriteLine("Isolated SQL integration database removed.");
        }

    }
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    sealed class SaveRaceBarrier : SaveChangesInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
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
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Ready.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return result;
        }
    }

}

public sealed class LocalReminderSqlFactAttribute : FactAttribute
{
    public LocalReminderSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MOTOGARAGE_RUN_SQL_TESTS") != "1")
            Skip = "Set MOTOGARAGE_RUN_SQL_TESTS=1 to run isolated local SQL Server transaction checks.";
    }
}
