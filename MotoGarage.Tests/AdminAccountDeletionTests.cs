using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotoGarage.API.Controllers;
using MotoGarage.API.Hubs;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Application.Services;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Notifications;
using Microsoft.Extensions.Options;

namespace MotoGarage.Tests;

public class AdminAccountDeletionTests
{
    internal static async Task Seed(AppDbContext context, UserRole role = UserRole.User)
    {
        using var users = TestSupport.Users(context);
        foreach (var user in new[] {
            new User { Id = 99, FullName = "Test Admin", Email = "admin@example.invalid", UserName = "admin@example.invalid", Role = UserRole.Admin, EmailConfirmed = true },
            new User { Id = 1, FullName = "Test Account", Email = "delete@example.invalid", UserName = "delete@example.invalid", Role = role, EmailConfirmed = true, PhoneNumber = "test-phone" },
            new User { Id = 2, FullName = "Counterparty", Email = "other@example.invalid", UserName = "other@example.invalid", Role = UserRole.Store, EmailConfirmed = true } })
            Assert.True((await users.CreateAsync(user, "pass123")).Succeeded);
        context.Stores.Add(new Store { Id = 20, UserId = role == UserRole.Store ? 1 : 2, StoreName = "Test Store", Slug = "test-store", Description = "Test", Address = "Test", PhoneNumber = "Test", IsActive = true });
        context.Mechanics.Add(new Mechanic { Id = 10, UserId = role == UserRole.Mechanic ? 1 : 2, BusinessName = "Test Mechanic", Slug = "test-mechanic", Description = "Test", Address = "Test", PhoneNumber = "Test", IsActive = true });
        context.Products.Add(new Product { Id = 30, StoreId = 20, Name = "Test Product", Slug = "test-product", Stock = 3, Price = 45, IsActive = true });
        context.Vehicles.Add(new Vehicle { Id = 40, UserId = 1, Brand = "Test", Model = "Car", Engine = "Test", VIN = "TESTVIN", PlateNumber = "TEST", Slug = "test-car" });
        context.Orders.Add(new Order { Id = 50, UserId = 1, StoreId = 20, Status = OrderStatus.Completed, TotalAmount = 45, OrderItems = [new OrderItem { ProductId = 30, Quantity = 1, UnitPrice = 45, TotalPrice = 45 }] });
        context.Appointments.Add(new Appointment { Id = 60, UserId = 1, MechanicId = 10, VehicleId = 40, Status = AppointmentStatus.Completed, AppointmentDate = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData(UserRole.User)]
    [InlineData(UserRole.Store)]
    [InlineData(UserRole.Mechanic)]
    public async Task DeletionRevokesAndAnonymizesAccountWithoutBreakingCompletedHistory(UserRole role)
    {
        using var context = TestSupport.Context();
        await Seed(context, role);
        using var users = TestSupport.Users(context);
        var target = await users.FindByIdAsync("1");
        await users.AddLoginAsync(target!, new UserLoginInfo("Google", "test-subject", "Google"));
        var auth = TestSupport.Auth(context, users);
        var login = await auth.LoginAsync(new() { Email = "delete@example.invalid", Password = "pass123" });
        context.EmailVerificationCodes.Add(new EmailVerificationCode { UserId = 1, CodeHash = "test", ExpiresAt = DateTime.UtcNow.AddMinutes(10) });
        context.Notifications.Add(new Notification { UserId = 1, Title = "Test", Message = "Test" });
        await context.SaveChangesAsync();
        var disconnected = false;
        var registry = new UserConnectionRegistry();
        registry.Register(1, "test-connection", () => disconnected = true);
        var service = new AdminService(context, users, new TestUnitOfWork(context), registry);
        await service.DeleteUserAsync(1, 99);
        context.ChangeTracker.Clear();
        var deleted = await context.Users.SingleAsync(x => x.Id == 1);
        Assert.NotNull(deleted.DeletedAtUtc);
        Assert.Equal(99, deleted.DeletedByAdminId);
        Assert.Equal("Silinmiş istifadəçi", deleted.FullName);
        Assert.Null(deleted.Email);
        Assert.Null(deleted.NormalizedEmail);
        Assert.Null(deleted.PhoneNumber);
        Assert.Null(deleted.PasswordHash);
        Assert.True(deleted.IsAdminBlocked);
        Assert.True(disconnected);
        Assert.Empty(context.UserLogins);
        Assert.Empty(context.EmailVerificationCodes);
        Assert.Empty(context.Notifications);
        Assert.All(context.RefreshTokens, token => Assert.NotNull(token.RevokedAt));
        Assert.DoesNotContain(await service.GetUsersAsync(null, null), user => user.Id == 1);
        Assert.Equal(2, (await service.GetDashboardAsync()).TotalUsers);
        Assert.Equal(OrderStatus.Completed, (await context.Orders.Include(x => x.User).SingleAsync()).Status);
        Assert.Equal(AppointmentStatus.Completed, (await context.Appointments.SingleAsync()).Status);
        Assert.Equal(3, (await context.Products.SingleAsync()).Stock);
        if (role == UserRole.Store) {
            Assert.False((await context.Stores.SingleAsync()).IsActive);
            Assert.False((await context.Products.SingleAsync()).IsActive);
        }
        if (role == UserRole.Mechanic) Assert.False((await context.Mechanics.SingleAsync()).IsActive);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(new() { Email = "delete@example.invalid", Password = "pass123" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.RefreshTokenAsync(new() { RefreshToken = login.RefreshToken }));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UnblockUserAsync(1));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.BlockUserAsync(1, 99));
        await service.DeleteUserAsync(1, 99);
        Assert.Equal(deleted.DeletedAtUtc, (await context.Users.SingleAsync(x => x.Id == 1)).DeletedAtUtc);
        var newAccount = await auth.RegisterAsync(new() { Email = "delete@example.invalid", FullName = "New Account", Password = "pass123", Role = UserRole.User });
        Assert.Equal("delete@example.invalid", newAccount.Email);
        Assert.NotEqual(1, (await users.FindByEmailAsync(newAccount.Email))!.Id);
    }

    [Fact]
    public async Task SelfOtherAdminsAndNonAdministratorCallersCannotDeleteAccounts()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        using var users = TestSupport.Users(context);
        var service = new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry());
        await Assert.ThrowsAsync<DomainException>(() => service.DeleteUserAsync(99, 99));
        context.Users.Add(new User { Id = 100, FullName = "Other Admin", Role = UserRole.Admin, EmailConfirmed = true });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainException>(() => service.DeleteUserAsync(100, 99));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteUserAsync(1, 2));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteUserAsync(1000, 99));
        Assert.All(context.Users, user => Assert.Null(user.DeletedAtUtc));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, false)] [InlineData(OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.Preparing, false)] [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Pending, true)] [InlineData(OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Preparing, true)] [InlineData(OrderStatus.Shipped, true)]
    public async Task ActiveBuyerOrStoreOrdersPreventDeletionWithoutChangingStock(OrderStatus status, bool store)
    {
        using var context = TestSupport.Context();
        await Seed(context, store ? UserRole.Store : UserRole.User);
        var order = await context.Orders.SingleAsync();
        order.UserId = store ? 2 : 1;
        order.Status = status;
        await context.SaveChangesAsync();
        using var users = TestSupport.Users(context);
        var service = new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry());
        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteUserAsync(1, 99));
        Assert.Null((await context.Users.SingleAsync(x => x.Id == 1)).DeletedAtUtc);
        Assert.Equal(status, (await context.Orders.SingleAsync()).Status);
        Assert.Equal(3, (await context.Products.SingleAsync()).Stock);
    }

    [Theory]
    [InlineData(AppointmentStatus.Pending, false)] [InlineData(AppointmentStatus.Accepted, false)]
    [InlineData(AppointmentStatus.Pending, true)] [InlineData(AppointmentStatus.Accepted, true)]
    public async Task ActiveCustomerOrMechanicAppointmentsPreventDeletion(AppointmentStatus status, bool mechanic)
    {
        using var context = TestSupport.Context();
        await Seed(context, mechanic ? UserRole.Mechanic : UserRole.User);
        var appointment = await context.Appointments.SingleAsync();
        appointment.UserId = mechanic ? 2 : 1;
        appointment.Status = status;
        await context.SaveChangesAsync();
        using var users = TestSupport.Users(context);
        await Assert.ThrowsAsync<ConflictException>(() => new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry()).DeleteUserAsync(1, 99));
        Assert.Null((await context.Users.SingleAsync(x => x.Id == 1)).DeletedAtUtc);
    }

    [Fact]
    public async Task DeletedAndAdminAccountsCannotCreateNewOrdersEvenIfEarlierAuthenticationPassed()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        using var users = TestSupport.Users(context);
        await new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry()).DeleteUserAsync(1, 99);
        var orders = TestSupport.Orders(context);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => orders.CreateAsync(1, new() { Items = [new() { ProductId = 30, Quantity = 1 }] }));
        await Assert.ThrowsAsync<ForbiddenException>(() => orders.CreateAsync(99, new() { Items = [new() { ProductId = 30, Quantity = 1 }] }));
        Assert.Single(context.Orders);
        Assert.Equal(3, (await context.Products.SingleAsync()).Stock);
    }

    [Fact]
    public async Task DeletedAndAdminAccountsCannotCreateAppointmentsOrReceiveGarageReminders()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        context.Reminders.Add(new Reminder { VehicleId = 40, Title = "Service", ReminderDate = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();
        using var users = TestSupport.Users(context);
        await new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry()).DeleteUserAsync(1, 99);
        var appointments = new AppointmentService(new AppointmentRepository(context), new MechanicRepository(context),
            new VehicleRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context),
            new NotificationService(new NotificationRepository(context), TestSupport.Mapper()), new AccountAccessService(context));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => appointments.CreateAsync(1, new() { MechanicId = 10, VehicleId = 40, AppointmentDate = DateTime.UtcNow.AddDays(1) }));
        await Assert.ThrowsAsync<ForbiddenException>(() => appointments.CreateAsync(99, new() { MechanicId = 10, VehicleId = 40, AppointmentDate = DateTime.UtcNow.AddDays(1) }));
        Assert.Single(context.Appointments);
        Assert.Equal(0, await new ReminderNotificationDispatcher(context, TimeProvider.System, Options.Create(new ReminderNotificationOptions())).DispatchDueAsync());
        Assert.Empty(context.Notifications);
    }

    [Fact]
    public async Task BlockedAdministratorCannotDeleteAnAccountAfterEarlierAuthentication()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var administrator = await context.Users.SingleAsync(x => x.Id == 99);
        administrator.IsAdminBlocked = true;
        await context.SaveChangesAsync();
        using var users = TestSupport.Users(context);
        await Assert.ThrowsAsync<ForbiddenException>(() => new AdminService(context, users, new TestUnitOfWork(context), new UserConnectionRegistry()).DeleteUserAsync(1, 99));
        Assert.Null((await context.Users.SingleAsync(x => x.Id == 1)).DeletedAtUtc);
    }

    [Fact]
    public void DeleteEndpointIsAdminOnlyAndPersonalControllersExcludeAdmin()
    {
        Assert.Equal("Admin", typeof(AdminController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles);
        foreach (var controller in new[] { typeof(VehicleController), typeof(FuelRecordController), typeof(ExpenseController), typeof(ServiceRecordController), typeof(ReminderController), typeof(OrderController), typeof(AppointmentController), typeof(ChatController), typeof(ReviewController), typeof(DashboardController), typeof(ChatHub) })
            Assert.DoesNotContain("Admin", controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles!);
    }
}
