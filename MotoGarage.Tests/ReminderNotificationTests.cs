using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotoGarage.API.HostedServices;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Notifications;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class ReminderNotificationTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true, false, false, false, 1)]
    [InlineData(false, true, false, false, 1)]
    [InlineData(true, true, false, false, 1)]
    [InlineData(false, false, false, false, 0)]
    [InlineData(true, true, true, false, 0)]
    [InlineData(true, true, false, true, 0)]
    public async Task DateOrOdometerTriggersOneNotificationForTheVehicleOwner(
        bool dateDue, bool kmDue, bool completed, bool sent, int expected)
    {
        await using var context = TestSupport.Context();
        await Seed(context, new Reminder
        {
            VehicleId = 2,
            Title = "Yağ dəyişimi",
            ReminderDate = dateDue ? Now : Now.AddDays(1),
            ReminderKm = kmDue ? 25000 : 30000,
            IsCompleted = completed,
            NotificationSentAtUtc = sent ? Now.AddMinutes(-1) : null
        });
        Assert.Equal(expected, await Dispatcher(context).DispatchDueAsync());
        if (expected == 1)
        {
            var notification = Assert.Single(context.Notifications);
            Assert.Equal(2, notification.UserId);
            Assert.Equal(NotificationType.Reminder, notification.Type);
            Assert.False(notification.IsRead);
            Assert.Equal(Now, notification.CreatedAt);
            Assert.Contains("Yağ dəyişimi", notification.Title);
            Assert.Equal("/garage/toyota-corolla-2/reminders", notification.Link);
            Assert.Equal(Now, (await context.Reminders.SingleAsync()).NotificationSentAtUtc);
        }
        else Assert.Empty(context.Notifications);
    }

    [Fact]
    public async Task DeliverySurvivesRestartAndReadingDoesNotResend()
    {
        var databaseOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var first = new AppDbContext(databaseOptions))
        {
            await Seed(first, new Reminder { VehicleId = 1, Title = "Baxış", ReminderDate = Now.AddDays(-1) });
            Assert.Equal(1, await Dispatcher(first).DispatchDueAsync());
            (await first.Notifications.SingleAsync()).IsRead = true;
            await first.SaveChangesAsync();
        }
        await using var restarted = new AppDbContext(databaseOptions);
        Assert.Equal(0, await Dispatcher(restarted).DispatchDueAsync());
        Assert.Single(restarted.Notifications);
        Assert.True((await restarted.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task TitleEditsDoNotResendButChangingTheDateRearmsDelivery()
    {
        await using var context = TestSupport.Context();
        await Seed(context, new Reminder { VehicleId = 1, Title = "Baxış", ReminderDate = Now });
        var clock = new ReminderClock(Now);
        var dispatcher = Dispatcher(context, clock);
        Assert.Equal(1, await dispatcher.DispatchDueAsync());
        var service = new ReminderService(new ReminderRepository(context), new VehicleRepository(context), TestSupport.Mapper());
        await service.UpdateAsync(1, 1, new() { Title = "Yeni başlıq", ReminderDate = Now });
        Assert.Equal(0, await dispatcher.DispatchDueAsync());
        Assert.NotNull((await context.Reminders.SingleAsync()).NotificationSentAtUtc);
        await service.UpdateAsync(1, 1, new() { Title = "Yeni başlıq", ReminderDate = Now.AddDays(1) });
        Assert.Null((await context.Reminders.SingleAsync()).NotificationSentAtUtc);
        Assert.Equal(0, await dispatcher.DispatchDueAsync());
        clock.UtcNow = new DateTimeOffset(Now.AddDays(1));
        Assert.Equal(1, await dispatcher.DispatchDueAsync());
        Assert.Equal(2, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task ChangingOdometerScheduleRearmsAndCompletionStopsDelivery()
    {
        await using var context = TestSupport.Context();
        var date = Now.AddYears(1);
        await Seed(context, new Reminder { VehicleId = 2, Title = "Baxış", ReminderDate = date, ReminderKm = 25000 });
        var dispatcher = Dispatcher(context);
        Assert.Equal(1, await dispatcher.DispatchDueAsync());
        var service = new ReminderService(new ReminderRepository(context), new VehicleRepository(context), TestSupport.Mapper());
        await service.UpdateAsync(1, 2, new() { Title = "Baxış", ReminderDate = date, ReminderKm = 30000 });
        Assert.Equal(0, await dispatcher.DispatchDueAsync());
        (await context.Vehicles.SingleAsync(x => x.Id == 2)).CurrentKm = 30000;
        await context.SaveChangesAsync();
        Assert.Equal(1, await dispatcher.DispatchDueAsync());
        await service.UpdateAsync(1, 2, new() { Title = "Baxış", ReminderDate = Now, ReminderKm = 30000 });
        await service.MarkAsCompletedAsync(1, 2);
        Assert.Equal(0, await dispatcher.DispatchDueAsync());
        Assert.Equal(2, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task BatchLimitsDrainTheBacklogWithoutDuplicates()
    {
        await using var context = TestSupport.Context();
        await Seed(context, Enumerable.Range(1, 5).Select(index => new Reminder
        {
            VehicleId = 1,
            Title = $"Baxış {index}",
            ReminderDate = Now.AddDays(-index)
        }).ToArray());
        var dispatcher = Dispatcher(context, batchSize: 2);
        Assert.Equal(2, await dispatcher.DispatchDueAsync());
        Assert.Equal(2, await dispatcher.DispatchDueAsync());
        Assert.Equal(1, await dispatcher.DispatchDueAsync());
        Assert.Equal(0, await dispatcher.DispatchDueAsync());
        Assert.Equal(5, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task CancellationDoesNotMarkOrDeliverReminders()
    {
        await using var context = TestSupport.Context();
        await Seed(context, new Reminder { VehicleId = 1, Title = "Baxış", ReminderDate = Now });
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Dispatcher(context).DispatchDueAsync(cancelled.Token));
        Assert.Empty(context.Notifications);
        Assert.Null((await context.Reminders.SingleAsync()).NotificationSentAtUtc);
    }

    [Fact]
    public void SqlServerClaimsGuardDuplicateAndStaleScheduleDelivery()
    {
        using var context = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
        var reminder = context.Model.FindEntityType(typeof(Reminder))!;
        foreach (var name in new[] { "NotificationSentAtUtc", "ReminderDate", "ReminderKm", "IsCompleted" })
            Assert.True(reminder.FindProperty(name)!.IsConcurrencyToken);
        var index = Assert.Single(reminder.GetIndexes(), x => x.GetDatabaseName() == "IX_Reminders_PendingNotification");
        Assert.Equal("[IsCompleted] = 0 AND [NotificationSentAtUtc] IS NULL", index.GetFilter());
    }

    [Fact]
    public async Task WorkerStartsImmediatelyRetriesFailuresAndDisposesEachScope()
    {
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var disposed = 0;
        var services = new ServiceCollection();
        services.AddScoped<IReminderNotificationDispatcher>(_ => new WorkerProbe(
            () =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                    throw new IOException("Test failure");
                recovered.TrySetResult();
                return Task.FromResult(0);
            }, () => Interlocked.Increment(ref disposed)));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var worker = new ReminderNotificationWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System, Options.Create(new ReminderNotificationOptions { PollIntervalSeconds = 1 }),
            NullLogger<ReminderNotificationWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        Assert.Equal(1, Volatile.Read(ref calls));
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        Assert.True(Volatile.Read(ref calls) >= 2);
        Assert.Equal(Volatile.Read(ref calls), Volatile.Read(ref disposed));
    }

    [Fact]
    public async Task DeletedReminderNotificationIsNotSentAgain()
    {
        using var context = TestSupport.Context();
        await Seed(context, new Reminder { Id = 1, VehicleId = 2, Title = "Due", ReminderDate = Now });
        Assert.Equal(1, await Dispatcher(context).DispatchDueAsync());
        var notification = await context.Notifications.SingleAsync();
        var service = new NotificationService(new NotificationRepository(context), TestSupport.Mapper());
        await service.DeleteAsync(notification.Id, notification.UserId);
        Assert.Empty(context.Notifications);
        Assert.NotNull((await context.Reminders.SingleAsync()).NotificationSentAtUtc);
        Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());
    }

    private static ReminderNotificationDispatcher Dispatcher(AppDbContext context, ReminderClock? clock = null, int batchSize = 100)
        => new(context, clock ?? new ReminderClock(Now), Options.Create(new ReminderNotificationOptions { BatchSize = batchSize }));

    private static async Task Seed(AppDbContext context, params Reminder[] reminders)
    {
        context.Users.AddRange(new User { Id = 1, FullName = "One" }, new User { Id = 2, FullName = "Two" });
        context.Vehicles.AddRange(Enumerable.Range(1, 2).Select(id => new Vehicle
        {
            Id = id,
            Slug = $"toyota-corolla-{id}",
            UserId = id,
            Brand = "Toyota",
            Model = "Corolla",
            VIN = $"VIN{id}",
            PlateNumber = $"PLATE{id}",
            Engine = "1.8",
            Year = 2022,
            CurrentKm = 25000,
            VehicleType = VehicleType.Car,
            FuelType = FuelType.Petrol
        }));
        context.Reminders.AddRange(reminders);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private sealed class ReminderClock(DateTime now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(now);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class WorkerProbe(Func<Task<int>> run, Action onDispose) : IReminderNotificationDispatcher, IDisposable
    {
        public Task<int> DispatchDueAsync(CancellationToken cancellationToken = default) => run();
        public void Dispose() => onDispose();
    }
}
