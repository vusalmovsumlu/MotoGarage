using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotoGarage.API.HostedServices;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Email;
using MotoGarage.Infrastructure.Notifications;

namespace MotoGarage.Tests;

public class AppointmentEmailReminderTests
{
    internal static readonly DateTime Now = new(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(AppointmentStatus.Pending, 7210, true, false)]
    [InlineData(AppointmentStatus.Accepted, 0, true, false)]
    [InlineData(AppointmentStatus.Accepted, -1, true, false)]
    [InlineData(AppointmentStatus.Cancelled, 3600, true, false)]
    [InlineData(AppointmentStatus.Completed, 3600, true, false)]
    [InlineData(AppointmentStatus.Rejected, 3600, true, false)]
    [InlineData(AppointmentStatus.Accepted, 3600, false, false)]
    [InlineData(AppointmentStatus.Accepted, 3600, true, true)]
    public async Task IneligibleAppointmentsNeverAttemptEmail(AppointmentStatus status, int seconds, bool confirmed, bool blocked)
    {
        using var context = TestSupport.Context();
        context.Users.Add(new User { Id = 1, FullName = "Customer", Email = "test@example.invalid", EmailConfirmed = confirmed, IsAdminBlocked = blocked });
        context.Appointments.Add(new Appointment { UserId = 1, Status = status, AppointmentDate = Now.AddSeconds(seconds) });
        await context.SaveChangesAsync();
        Assert.Equal(0, await Dispatcher(context, new ReminderMail(), new ReminderClock(Now)).DispatchDueAsync());
    }

    [Fact]
    public void MailContainsBakuDateAnd24HourTimeAndEscapesUserText()
    {
        var message = AppointmentReminderEmailBuilder.Create(new(1, "Vüsal <script>alert(1)</script>", Now, "Toyota Corolla", "Əli Məmmədov", "Bakı & Yasamal", AppointmentStatus.Pending));
        Assert.Contains("10 oktyabr 2026, 22:00", message.TextBody);
        Assert.Contains("Toyota Corolla", message.TextBody);
        Assert.Contains("Əli Məmmədov", message.TextBody);
        Assert.Contains("Bakı vaxtı", message.TextBody);
        Assert.Contains("hələ görüşü təsdiqləməyib", message.TextBody);
        Assert.DoesNotContain("<script>", message.HtmlBody);
        Assert.Contains("&lt;script&gt;", message.HtmlBody);
        Assert.Contains("Bakı &amp; Yasamal", message.HtmlBody);
        var midnight = AppointmentReminderEmailBuilder.Create(new(1, "Customer", Now.AddHours(2), "Car", "Mechanic", "Address", AppointmentStatus.Accepted));
        Assert.Contains("11 oktyabr 2026, 00:00", midnight.TextBody);
        Assert.Contains("qəbul edilib", midnight.TextBody);
    }

    [Fact]
    public async Task HostedWorkerDispatchesWithoutAnyBrowserAndStopsCleanly()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcher = new WorkerProbe(ready);
        var services = new ServiceCollection().AddSingleton<IAppointmentEmailReminderDispatcher>(dispatcher);
        using var provider = services.BuildServiceProvider();
        using var worker = new AppointmentEmailReminderWorker(provider.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System,
            Options.Create(new AppointmentEmailReminderOptions()), NullLogger<AppointmentEmailReminderWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        Assert.Equal(1, dispatcher.Calls);
    }

    internal static AppointmentEmailReminderDispatcher Dispatcher(MotoGarage.Infrastructure.Data.AppDbContext context, ReminderMail email, ReminderClock clock, int batchSize = 25) => new(
        context, email, clock, Options.Create(new AppointmentEmailReminderOptions { BatchSize = batchSize }), NullLogger<AppointmentEmailReminderDispatcher>.Instance);

    internal sealed class ReminderClock(DateTime now) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }

    internal sealed class ReminderMail : IEmailService
    {
        public Task SendWorkflowUpdateAsync(string email, MotoGarage.Application.DTOs.Notification.WorkflowEmailContent content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public System.Collections.Concurrent.ConcurrentQueue<(string Recipient, AppointmentReminderEmail Reminder)> Sent { get; } = new();
        public int Attempts;
        public bool Fail { get; set; }
        public Task SendAppointmentReminderAsync(string email, AppointmentReminderEmail reminder, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Attempts);
            if (Fail) throw new IOException("Simulated SMTP failure");
            Sent.Enqueue((email, reminder));
            return Task.CompletedTask;
        }
        public Task SendVerificationCodeAsync(string email, string fullName, string code) => throw new NotSupportedException();
        public Task SendVehicleHistoryAsync(string email, string vehicleName, byte[] attachment, string fileName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    sealed class WorkerProbe(TaskCompletionSource ready) : IAppointmentEmailReminderDispatcher
    {
        public int Calls;
        public Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls); ready.TrySetResult(); return Task.FromResult(1);
        }
    }
}
