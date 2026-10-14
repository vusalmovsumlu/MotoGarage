using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotoGarage.API.HostedServices;
using MotoGarage.Application.DTOs.Notification;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Email;
using MotoGarage.Infrastructure.Inventory;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Scheduling;

namespace MotoGarage.Tests;

public class WorkflowEmailTests
{
    internal static async Task Seed(AppDbContext context)
    {
        var customer = new User { FullName = "Vüsal Mövsümlü", Email = "customer@example.invalid", EmailConfirmed = true, Role = UserRole.User };
        var mechanicUser = new User { FullName = "Əli Məmmədov", EmailConfirmed = true, Role = UserRole.Mechanic };
        var storeUser = new User { FullName = "Satıcı", EmailConfirmed = true, Role = UserRole.Store };
        context.Users.AddRange(customer, mechanicUser, storeUser);
        var mechanic = new Mechanic { User = mechanicUser, BusinessName = "Moto & Auto Servis", Slug = "servis", Address = "Bakı & Yasamal", Description = "Servis", PhoneNumber = "123", IsActive = true };
        var store = new Store { User = storeUser, StoreName = "Garage Parts", Slug = "shop", Address = "Bakı", Description = "Parts", PhoneNumber = "123", IsActive = true };
        context.Mechanics.Add(mechanic);
        context.Stores.Add(store);
        context.Vehicles.Add(new Vehicle { User = customer, Brand = "Toyota", Model = "Corolla", Slug = "car", PlateNumber = "10-AA-100", VIN = "TESTVIN", Engine = "1.8", Year = 2022 });
        context.Products.Add(new Product { Store = store, Name = "Motor oil", Slug = "oil", Stock = 10, Price = 10, IsActive = true });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    internal static OrderService Orders(AppDbContext c, IUnitOfWork? work = null) => new(new OrderRepository(c), new ProductRepository(c), new StoreRepository(c), TestSupport.Mapper(),
        work ?? new TestUnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), new AccountAccessService(c), new StockAlertService(c), TestSupport.WorkflowEmails(c));
    internal static AppointmentPlanningService Planning(AppDbContext c, IUnitOfWork? work = null) => new(c, work ?? new TestUnitOfWork(c), new AccountAccessService(c), TestSupport.Mapper(), TestSupport.WorkflowEmails(c));
    internal static AppointmentService Appointments(AppDbContext c, IUnitOfWork? work = null) => new(new AppointmentRepository(c), new MechanicRepository(c), new VehicleRepository(c), TestSupport.Mapper(),
        work ?? new TestUnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), new AccountAccessService(c), Planning(c, work), TestSupport.WorkflowEmails(c));

    [Fact]
    public async Task EveryOrderStepQueuesItsOwnSnapshotAndRepeatedCallsCannotDuplicateIt()
    {
        using var c = TestSupport.Context(); await Seed(c);
        var service = Orders(c);
        var order = await service.CreateAsync(1, TestSupport.Order());
        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Completed })
        {
            await service.UpdateStatusAsync(order.Id, 3, new() { Status = status });
            await Assert.ThrowsAsync<DomainException>(() => service.UpdateStatusAsync(order.Id, 3, new() { Status = status }));
            await TestSupport.WorkflowEmails(c).OrderAsync(order.Id);
        }
        var messages = await c.WorkflowEmails.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(5, messages.Count);
        Assert.Equal(5, messages.Select(x => x.DeliveryId).Distinct().Count());
        Assert.Equal(new[] { "yaradıldı", "təsdiqləndi", "hazırlanır", "göndərildi", "tamamlandı" },
            messages.Select(x => new[] { "yaradıldı", "təsdiqləndi", "hazırlanır", "göndərildi", "tamamlandı" }.Single(word => x.TextBody.Contains(word))));
        Assert.All(messages, x =>
        {
            Assert.Equal(1, x.RecipientUserId);
            Assert.Contains("Garage Parts", x.TextBody);
            Assert.Contains("Motor oil × 1", x.TextBody);
            Assert.Contains("10,00 ₼", x.TextBody);
            Assert.Null(x.SentAtUtc);
        });
    }

    [Theory]
    [InlineData(AppointmentStatus.Accepted)]
    [InlineData(AppointmentStatus.Rejected)]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    public async Task AppointmentCreationAndEveryAllowedOutcomeQueuesCustomerEmail(AppointmentStatus status)
    {
        using var c = TestSupport.Context(); await Seed(c);
        var service = Appointments(c);
        var appointment = await service.CreateAsync(1, new() { MechanicId = 1, VehicleId = 1, AppointmentDate = BusinessOperationsTests.Slot() });
        if (status == AppointmentStatus.Completed)
            await service.UpdateStatusAsync(appointment.Id, 2, new() { Status = AppointmentStatus.Accepted });
        if (status == AppointmentStatus.Cancelled)
        {
            await service.CancelAsync(appointment.Id, 1);
            await service.CancelAsync(appointment.Id, 1);
        }
        else await service.UpdateStatusAsync(appointment.Id, 2, new() { Status = status });
        await TestSupport.WorkflowEmails(c).AppointmentAsync(appointment.Id);
        var messages = await c.WorkflowEmails.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(status == AppointmentStatus.Completed ? 3 : 2, messages.Count);
        Assert.Contains(WorkflowEmailBuilder.AppointmentStatusText(status), messages.Last().TextBody);
        Assert.All(messages, x =>
        {
            Assert.Equal(1, x.RecipientUserId);
            Assert.Contains("Toyota Corolla", x.TextBody);
            Assert.Contains("Əli Məmmədov", x.TextBody);
            Assert.Contains("Moto & Auto Servis", x.TextBody);
            Assert.Contains("10:00 (Bakı vaxtı)", x.TextBody);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomerAndStoreCancellationSendExactlyOneCancellationUpdate(bool storeCancels)
    {
        using var c = TestSupport.Context(); await Seed(c);
        var service = Orders(c);
        var order = await service.CreateAsync(1, TestSupport.Order());
        if (storeCancels) await service.UpdateStatusAsync(order.Id, 3, new() { Status = OrderStatus.Cancelled });
        else await service.CancelAsync(order.Id, 1);
        await service.CancelAsync(order.Id, 1);
        Assert.Equal(2, await c.WorkflowEmails.CountAsync());
        Assert.Equal(10, (await c.Products.SingleAsync()).Stock);
        Assert.Contains("ləğv edildi", (await c.WorkflowEmails.OrderBy(x => x.Id).LastAsync()).TextBody);
    }

    [Fact]
    public async Task OnlyAnAgreedNewAppointmentTimeQueuesARescheduleEmail()
    {
        using var c = TestSupport.Context(); await Seed(c);
        var appointment = await Appointments(c).CreateAsync(1, new() { MechanicId = 1, VehicleId = 1, AppointmentDate = BusinessOperationsTests.Slot() });
        var planning = Planning(c);
        await planning.ProposeAsync(appointment.Id, 1, new() { AppointmentDate = BusinessOperationsTests.Slot().AddHours(2) });
        Assert.Equal(1, await c.WorkflowEmails.CountAsync());
        await planning.DecideRescheduleAsync(appointment.Id, 2, new() { Approve = false, Revision = 1 });
        Assert.Equal(1, await c.WorkflowEmails.CountAsync());
        await planning.ProposeAsync(appointment.Id, 2, new() { AppointmentDate = BusinessOperationsTests.Slot().AddHours(3) });
        await planning.DecideRescheduleAsync(appointment.Id, 1, new() { Approve = true, Revision = 2 });
        var timeChange = await c.WorkflowEmails.SingleAsync(x => x.EventKey.EndsWith(":time:2"));
        Assert.Contains("13:00 (Bakı vaxtı)", timeChange.TextBody);
        Assert.Contains("qarşılıqlı razılıqla", timeChange.TextBody);
        await Assert.ThrowsAsync<ConflictException>(() => planning.DecideRescheduleAsync(appointment.Id, 1, new() { Approve = true, Revision = 2 }));
        Assert.Equal(2, await c.WorkflowEmails.CountAsync());
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("deleted")]
    [InlineData("unconfirmed")]
    [InlineData("missing")]
    public async Task IneligibleRecipientsDoNotReceiveNewQueuedEmail(string condition)
    {
        using var c = TestSupport.Context(); await Seed(c);
        var order = await Orders(c).CreateAsync(1, TestSupport.Order());
        var user = await c.Users.FindAsync(1);
        if (condition == "blocked") user!.IsAdminBlocked = true;
        if (condition == "deleted") user!.DeletedAtUtc = DateTime.UtcNow;
        if (condition == "unconfirmed") user!.EmailConfirmed = false;
        if (condition == "missing") user!.Email = null;
        await c.SaveChangesAsync();
        await Orders(c).UpdateStatusAsync(order.Id, 3, new() { Status = OrderStatus.Confirmed });
        Assert.Single(await c.WorkflowEmails.ToListAsync());
    }

    [Fact]
    public void EmailEscapesNamesAndDisplaysBakuTimeWithoutAmPm()
    {
        var body = WorkflowEmailBuilder.Build("Vüsal <script>", "Sifariş statusu", "Hazırlanır",
            [("Məhsul", "Oil <img src=x> & Filter\n2 ədəd")], new DateTime(2026, 10, 14, 19, 30, 0, DateTimeKind.Utc));
        Assert.Contains("15 oktyabr 2026, 23:30", WorkflowEmailBuilder.BakuDate(new DateTime(2026, 10, 15, 19, 30, 0, DateTimeKind.Utc)));
        Assert.Contains("14 oktyabr 2026, 23:30", body.Text);
        Assert.DoesNotContain("<script>", body.Html);
        Assert.DoesNotContain("<img", body.Html);
        Assert.Contains("&lt;script&gt;", body.Html);
        Assert.Contains("&amp; Filter<br>2 ədəd", body.Html);
    }

    [Fact]
    public async Task WorkerDispatchesWithoutBrowserAndStopsCleanly()
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var services = new ServiceCollection().AddSingleton<IWorkflowEmailDispatcher>(new WorkerProbe(ready));
        using var provider = services.BuildServiceProvider();
        using var worker = new WorkflowEmailWorker(provider.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System,
            Options.Create(new WorkflowEmailOptions()), NullLogger<WorkflowEmailWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
    }

    internal sealed class Mail : IEmailService
    {
        public ConcurrentQueue<(string Recipient, WorkflowEmailContent Content)> Sent { get; } = new();
        public int Attempts;
        public bool Fail { get; set; }
        public Task SendWorkflowUpdateAsync(string email, WorkflowEmailContent content, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Attempts);
            if (Fail) throw new IOException("Simulated SMTP failure");
            Sent.Enqueue((email, content)); return Task.CompletedTask;
        }
        public Task SendAppointmentReminderAsync(string email, MotoGarage.Application.DTOs.Appointment.AppointmentReminderEmail reminder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SendVehicleHistoryAsync(string email, string vehicleName, byte[] attachment, string fileName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SendVerificationCodeAsync(string email, string fullName, string code) => throw new NotSupportedException();
    }

    sealed class WorkerProbe(TaskCompletionSource ready) : IWorkflowEmailDispatcher
    {
        public Task<int> DispatchDueAsync(CancellationToken cancellationToken = default) { ready.TrySetResult(); return Task.FromResult(0); }
    }
}
