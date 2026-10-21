using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Email;
using MotoGarage.Infrastructure.Notifications;
using MotoGarage.Infrastructure.Repositories;
using Clock = MotoGarage.Tests.AppointmentEmailReminderTests.ReminderClock;

namespace MotoGarage.Tests;

public class ReminderEmailTests
{
    internal static readonly DateTime Now = new(2026, 10, 21, 19, 30, 0, DateTimeKind.Utc);
    internal static ReminderNotificationDispatcher Dispatcher(AppDbContext context, Clock? clock = null) =>
        new(context, clock ?? new Clock(Now), Options.Create(new ReminderNotificationOptions()));
    internal static ReminderService Service(AppDbContext context) =>
        new(new ReminderRepository(context), new VehicleRepository(context), TestSupport.Mapper());

    internal static async Task<Reminder> Seed(AppDbContext context, DateTime? date = null, int? km = null)
    {
        var vehicle = new Vehicle
        {
            User = new User { FullName = "Vüsal <test>", Email = "reminder@example.invalid", EmailConfirmed = true },
            Brand = "Honda", Model = "Civic", VIN = "REMINDER-EMAIL", PlateNumber = "10-AA-100",
            Slug = "honda-civic", Engine = "1.8", Year = 2022, CurrentKm = 25000
        };
        var reminder = new Reminder
        {
            Vehicle = vehicle, Title = "Yağ & filtr", Description = "Dəyiş <img src=x>",
            ReminderDate = date ?? Now, ReminderKm = km
        };
        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return reminder;
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public async Task DateOrOdometerQueuesOneEmailAndOneNotification(bool dateDue, bool kmDue, bool expected)
    {
        using var context = TestSupport.Context();
        var reminder = await Seed(context, dateDue ? Now : Now.AddDays(1), kmDue ? 25000 : 30000);
        Assert.Equal(expected ? 1 : 0, await Dispatcher(context).DispatchDueAsync());
        Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());
        Assert.Equal(expected ? 1 : 0, await context.Notifications.CountAsync());
        Assert.Equal(expected ? 1 : 0, await context.WorkflowEmails.CountAsync());
        if (!expected) return;
        var message = await context.WorkflowEmails.SingleAsync();
        Assert.Equal(reminder.Vehicle.UserId, message.RecipientUserId);
        Assert.Equal($"reminder:{reminder.Id}:due:0", message.EventKey);
        Assert.Contains("Honda Civic (10-AA-100)", message.TextBody);
        Assert.Contains("23:30 (Bakı vaxtı)", message.TextBody);
        Assert.Contains(dateDue ? "tarixi" : "odometr həddi", message.TextBody);
        Assert.Contains("Yağ &amp; filtr", message.HtmlBody);
        Assert.DoesNotContain("<test>", message.HtmlBody);
        Assert.DoesNotContain("<img", message.HtmlBody);
        Assert.Contains("&lt;img src=x&gt;", message.HtmlBody);
        Assert.Equal(Now, (await context.Reminders.SingleAsync()).EmailQueuedAtUtc);
        Assert.Null(message.SentAtUtc);
    }

    [Theory]
    [InlineData("unverified", 1)]
    [InlineData("blocked", 1)]
    [InlineData("missing", 1)]
    [InlineData("deleted", 0)]
    [InlineData("completed", 0)]
    public async Task IneligibleAccountsAndCompletedRemindersDoNotQueueMail(string condition, int notifications)
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var reminder = await context.Reminders.Include(x => x.Vehicle.User).SingleAsync();
        var owner = reminder.Vehicle.User;
        if (condition == "unverified") owner.EmailConfirmed = false;
        if (condition == "blocked") owner.IsAdminBlocked = true;
        if (condition == "missing") owner.Email = null;
        if (condition == "deleted") owner.DeletedAtUtc = Now;
        if (condition == "completed") reminder.IsCompleted = true;
        await context.SaveChangesAsync();
        await Dispatcher(context).DispatchDueAsync();
        Assert.Empty(context.WorkflowEmails);
        Assert.Equal(notifications, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task CatchUpAndRestartDoNotRepeatTheExistingNotificationOrEmail()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var context = new AppDbContext(options))
        {
            await Seed(context);
            (await context.Reminders.SingleAsync()).NotificationSentAtUtc = Now.AddDays(-1);
            await context.SaveChangesAsync();
            Assert.Equal(1, await Dispatcher(context).DispatchDueAsync());
            Assert.Empty(context.Notifications);
            Assert.Single(context.WorkflowEmails);
        }
        await using var restarted = new AppDbContext(options);
        Assert.Equal(0, await Dispatcher(restarted).DispatchDueAsync());
        Assert.Single(restarted.WorkflowEmails);
    }

    [Fact]
    public async Task OnlyScheduleChangesRearmEmailAndReturningToTheOriginalDateUsesANewEvent()
    {
        using var context = TestSupport.Context();
        var reminder = await Seed(context);
        var service = Service(context);
        await Dispatcher(context).DispatchDueAsync();
        await service.UpdateAsync(reminder.Id, reminder.Vehicle.UserId, new() { Title = "Yeni başlıq", ReminderDate = Now });
        Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());
        Assert.Equal(0, (await context.Reminders.SingleAsync()).ScheduleRevision);
        await service.UpdateAsync(reminder.Id, reminder.Vehicle.UserId, new() { Title = "Baxış", ReminderDate = Now.AddDays(1) });
        Assert.Null((await context.Reminders.SingleAsync()).EmailQueuedAtUtc);
        Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());
        await service.UpdateAsync(reminder.Id, reminder.Vehicle.UserId, new() { Title = "Baxış", ReminderDate = Now });
        Assert.Equal(1, await Dispatcher(context).DispatchDueAsync());
        Assert.Equal(new[] { $"reminder:{reminder.Id}:due:0", $"reminder:{reminder.Id}:due:2" },
            await context.WorkflowEmails.OrderBy(x => x.Id).Select(x => x.EventKey).ToArrayAsync());
        await service.UpdateAsync(reminder.Id, reminder.Vehicle.UserId, new() { Title = "Baxış", ReminderDate = Now.AddDays(1), ReminderKm = 26000 });
        Assert.Equal(0, await Dispatcher(context).DispatchDueAsync());
        (await context.Vehicles.SingleAsync()).CurrentKm = 26000;
        await context.SaveChangesAsync();
        Assert.Equal(1, await Dispatcher(context).DispatchDueAsync());
        Assert.Equal(3, await context.WorkflowEmails.CountAsync());
    }

    [Fact]
    public void ClaimsAndScheduleRevisionAreGuardedBySqlConcurrencyTokens()
    {
        using var context = new AppDbContextFactory().CreateDbContext([]);
        var reminder = context.Model.FindEntityType(typeof(Reminder))!;
        Assert.True(reminder.FindProperty(nameof(Reminder.EmailQueuedAtUtc))!.IsConcurrencyToken);
        Assert.True(reminder.FindProperty(nameof(Reminder.ScheduleRevision))!.IsConcurrencyToken);
        Assert.Contains(reminder.GetIndexes(), x => x.GetDatabaseName() == "IX_Reminders_PendingEmail");
    }
}
