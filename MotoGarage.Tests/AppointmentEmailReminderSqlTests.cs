using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;
using static MotoGarage.Tests.AppointmentEmailReminderTests;

namespace MotoGarage.Tests;

public class AppointmentEmailReminderSqlTests
{
    [LocalReminderSqlFact]
    public async Task MigrationTimingConcurrentClaimsRestartRetryAndCancellationUseRealSqlWithoutRealMail()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_AppointmentEmailChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        DbContextOptions<AppDbContext> Options(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString);
            if (interceptor != null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }
        await using var context = new AppDbContext(Options());
        try
        {
            var previous = context.Database.GetMigrations().TakeWhile(x => !x.EndsWith("AddAppointmentEmailReminders")).Last();
            await context.GetService<IMigrator>().MigrateAsync(previous);
            var oldDate = Now.AddHours(3);
            // Seed the historical schema without asking the current EF model to write future columns.
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AspNetUsers] ([FullName],[Email],[Role],[BusinessApprovalStatus],[CreatedAt],[EmailConfirmed],[IsAdminBlocked],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
                VALUES (N'Vüsal Mövsümlü','owner@example.invalid',1,2,{Now},1,0,0,0,0,0);
                DECLARE @owner int = SCOPE_IDENTITY();
                INSERT INTO [AspNetUsers] ([FullName],[Email],[Role],[BusinessApprovalStatus],[CreatedAt],[EmailConfirmed],[IsAdminBlocked],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
                VALUES (N'Əli Məmmədov','mechanic@example.invalid',2,2,{Now},1,0,0,0,0,0);
                DECLARE @mechanicUser int = SCOPE_IDENTITY();
                INSERT INTO [Mechanics] ([UserId],[BusinessName],[Slug],[Description],[Address],[PhoneNumber],[Rating],[IsActive],[CreatedAt])
                VALUES (@mechanicUser,N'Servis','servis',N'Servis',N'Bakı','123',0,1,{Now});
                DECLARE @mechanic int = SCOPE_IDENTITY();
                INSERT INTO [Vehicles] ([UserId],[Brand],[Model],[Slug],[VIN],[PlateNumber],[Engine],[Year],[CurrentKm],[VehicleType],[FuelType],[CreatedAt])
                VALUES (@owner,'Toyota','Corolla','toyota','TESTVIN','TEST','1.8',2022,0,1,1,{Now});
                DECLARE @vehicle int = SCOPE_IDENTITY();
                INSERT INTO [Appointments] ([UserId],[MechanicId],[VehicleId],[AppointmentDate],[Status],[CreatedAt],[Description])
                VALUES (@owner,@mechanic,@vehicle,{oldDate},{(int)AppointmentStatus.Pending},{Now},N'Existing appointment');
                """);
            await context.Database.MigrateAsync();
            var owner = await context.Users.SingleAsync(x => x.Email == "owner@example.invalid");
            var mechanic = await context.Mechanics.SingleAsync();
            var vehicle = await context.Vehicles.SingleAsync();
            var existing = await context.Appointments.AsNoTracking().SingleAsync();
            Assert.Equal("Existing appointment", existing.Description);
            Assert.Equal(oldDate, existing.AppointmentDate);
            Assert.Null(existing.EmailReminderSentAtUtc);
            Assert.Null(existing.EmailReminderLeaseId);

            Appointment Add(DateTime date, AppointmentStatus status = AppointmentStatus.Accepted) => new()
            { UserId = owner.Id, MechanicId = mechanic.Id, VehicleId = vehicle.Id, AppointmentDate = date, Status = status, CreatedAt = Now };
            var soon = Add(Now.AddHours(1));
            var exactTwoHours = Add(Now.AddHours(2), AppointmentStatus.Pending);
            var tooEarly = Add(Now.AddHours(2).AddSeconds(1));
            var expiredLease = Add(Now.AddMinutes(80));
            expiredLease.EmailReminderLeaseId = Guid.NewGuid();
            expiredLease.EmailReminderLeaseUntilUtc = Now.AddMinutes(-1);
            context.Appointments.AddRange(soon, exactTwoHours, tooEarly, expiredLease,
                Add(Now), Add(Now.AddMinutes(-1)), Add(Now.AddMinutes(40), AppointmentStatus.Cancelled),
                Add(Now.AddMinutes(41), AppointmentStatus.Completed), Add(Now.AddMinutes(42), AppointmentStatus.Rejected));
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var mail = new ReminderMail();
            var clock = new ReminderClock(Now);

            // Both workers see the same first candidate before either can UPDATE its lease.
            var barrier = new CandidateBarrier();
            await using var first = new AppDbContext(Options(barrier));
            await using var second = new AppDbContext(Options(barrier));
            var counts = await Task.WhenAll(Dispatcher(first, mail, clock, 1).DispatchDueAsync(), Dispatcher(second, mail, clock, 1).DispatchDueAsync());
            Assert.Equal(1, counts.Sum());
            Assert.Equal(soon.Id, Assert.Single(mail.Sent).Reminder.AppointmentId);
            Assert.Equal(2, await Dispatcher(context, mail, clock).DispatchDueAsync());
            Assert.Equal(3, mail.Sent.Count);
            Assert.Equal(3, mail.Sent.Select(x => x.Reminder.AppointmentId).Distinct().Count());
            Assert.All(mail.Sent, item =>
            {
                Assert.Equal("owner@example.invalid", item.Recipient);
                Assert.Equal("Toyota Corolla", item.Reminder.VehicleName);
                Assert.Equal("Əli Məmmədov", item.Reminder.MechanicName);
            });
            Assert.Null((await context.Appointments.AsNoTracking().SingleAsync(x => x.Id == tooEarly.Id)).EmailReminderSentAtUtc);
            await using (var restarted = new AppDbContext(Options()))
                Assert.Equal(0, await Dispatcher(restarted, mail, clock).DispatchDueAsync());

            // Keep the outside-window fixture out of the upcoming retry interval.
            await context.Appointments.Where(x => x.Id == tooEarly.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, AppointmentStatus.Cancelled));
            var retry = Add(Now.AddMinutes(70));
            context.Appointments.Add(retry); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
            mail.Fail = true;
            Assert.Equal(0, await Dispatcher(context, mail, clock).DispatchDueAsync());
            var failed = await context.Appointments.AsNoTracking().SingleAsync(x => x.Id == retry.Id);
            Assert.Null(failed.EmailReminderSentAtUtc);
            Assert.Null(failed.EmailReminderLeaseId);
            Assert.Equal(Now.AddMinutes(5), failed.EmailReminderNextAttemptAtUtc);
            mail.Fail = false;
            clock.UtcNow = Now.AddMinutes(5).AddSeconds(-1);
            Assert.Equal(0, await Dispatcher(context, mail, clock).DispatchDueAsync());
            Assert.Equal(4, mail.Attempts);
            clock.UtcNow = Now.AddMinutes(5);
            Assert.Equal(1, await Dispatcher(context, mail, clock).DispatchDueAsync());
            Assert.Equal(5, mail.Attempts);

            var cancelling = Add(Now.AddMinutes(75));
            context.Appointments.Add(cancelling); await context.SaveChangesAsync(); context.ChangeTracker.Clear();
            var cancel = new AfterClaim(async () =>
            {
                await using var other = new AppDbContext(Options());
                await other.Appointments.Where(x => x.Id == cancelling.Id).ExecuteUpdateAsync(set => set.SetProperty(x => x.Status, AppointmentStatus.Cancelled));
            });
            await using (var racing = new AppDbContext(Options(cancel)))
                Assert.Equal(0, await Dispatcher(racing, mail, clock).DispatchDueAsync());
            Assert.Equal(5, mail.Attempts);
            var cancelled = await context.Appointments.AsNoTracking().SingleAsync(x => x.Id == cancelling.Id);
            Assert.Null(cancelled.EmailReminderLeaseId);
            Assert.Null(cancelled.EmailReminderSentAtUtc);
        }
        finally
        {
            var target = new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog;
            if (target != databaseName || !target.StartsWith("MotoGarage_AppointmentEmailChecks_") || target == "MotoGarageDb")
                throw new InvalidOperationException("Unsafe test database cleanup target.");
            await context.Database.EnsureDeletedAsync();
        }
    }

    sealed class CandidateBarrier : DbCommandInterceptor
    {
        int arrivals;
        readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("SELECT TOP") && command.CommandText.Contains("[Appointments]") && Interlocked.Increment(ref arrivals) <= 2)
            {
                if (Volatile.Read(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }

    sealed class AfterClaim(Func<Task> cancel) : DbCommandInterceptor
    {
        int called;
        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("EmailReminderLeaseUntilUtc") && Interlocked.Exchange(ref called, 1) == 0 && result == 1)
                await cancel();
            return result;
        }
    }
}
