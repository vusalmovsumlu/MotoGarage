using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Inventory;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Scheduling;

namespace MotoGarage.Tests;

public class BusinessOperationsTests
{
    internal static DateTime Slot()
    {
        var day = DateTime.UtcNow.AddHours(4).Date.AddDays(2);
        if (day.DayOfWeek == DayOfWeek.Sunday) day = day.AddDays(1);
        return DateTime.SpecifyKind(day.AddHours(6), DateTimeKind.Utc); // 10:00 Baku
    }
    internal static async Task Seed(AppDbContext context)
    {
        context.Users.AddRange(new User { Id = 1, FullName = "Owner", EmailConfirmed = true },
            new User { Id = 2, FullName = "Mechanic", Role = UserRole.Mechanic, EmailConfirmed = true },
            new User { Id = 3, FullName = "Other", EmailConfirmed = true },
            new User { Id = 4, FullName = "Store", Role = UserRole.Store, EmailConfirmed = true },
            new User { Id = 99, FullName = "Admin", Role = UserRole.Admin, EmailConfirmed = true });
        context.Mechanics.Add(new Mechanic { Id = 10, UserId = 2, BusinessName = "Garage", Slug = "garage", Description = "Service", PhoneNumber = "123", Address = "Baku", IsActive = true });
        context.Vehicles.AddRange(new Vehicle { Id = 5, UserId = 1, Brand = "Toyota", Model = "Corolla", Slug = "toyota", VIN = "VINONE", PlateNumber = "ONE", Engine = "1.8", CurrentKm = 1000 },
            new Vehicle { Id = 6, UserId = 3, Brand = "BMW", Model = "320", Slug = "bmw", VIN = "VINTWO", PlateNumber = "TWO", Engine = "2.0" });
        context.Appointments.Add(new Appointment { Id = 20, UserId = 1, MechanicId = 10, VehicleId = 5, AppointmentDate = Slot(), Status = AppointmentStatus.Accepted, CreatedAt = DateTime.UtcNow });
        context.Stores.Add(new Store { Id = 30, UserId = 4, StoreName = "Shop", Slug = "shop", Address = "Baku", Description = "Parts", PhoneNumber = "123", IsActive = true });
        context.Products.Add(new Product { Id = 40, StoreId = 30, Name = "Oil", Slug = "oil", Stock = 8, LowStockThreshold = 5, Price = 10, IsActive = true });
        foreach (var entry in context.ChangeTracker.Entries<User>()) { entry.Entity.SecurityStamp = Guid.NewGuid().ToString(); entry.Entity.UserName = "test-" + entry.Entity.Id; }
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
    }
    static SubmitServiceReportDto Report(int revision = 0) => new()
    { ServiceName = "Oil change", Description = "Engine service", ReplacedParts = "Oil filter", Cost = 80, Kilometer = 1200, ServiceDate = DateTime.UtcNow.AddHours(-1), Revision = revision };
    static async Task Complete(AppDbContext context)
    {
        var a = await context.Appointments.FindAsync(20); a!.Status = AppointmentStatus.Completed; a.CompletedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task AvailabilityExcludesOverlapsAndAdjacentSlotRemainsFree()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var service = TestSupport.Planning(context); var slot = Slot();
        var result = await service.GetAvailabilityAsync(10, DateOnly.FromDateTime(slot.AddHours(4)), 60);
        Assert.Equal("Asia/Baku", result.TimeZone);
        Assert.DoesNotContain(slot, result.Slots); Assert.DoesNotContain(slot.AddMinutes(45), result.Slots);
        Assert.Contains(slot.AddHours(1), result.Slots);
        await Assert.ThrowsAsync<ConflictException>(() => service.ValidateSlotAsync(10, 6, slot.AddMinutes(15), 60));
        await service.ValidateSlotAsync(10, 6, slot.AddHours(1), 60);
    }
    [Fact]
    public async Task WorkingHoursValidateAndApplyOnlyToNewBookings()
    {
        using var context = TestSupport.Context(); await Seed(context); var service = TestSupport.Planning(context);
        var hours = await service.GetWorkingHoursAsync(2);
        await Assert.ThrowsAsync<DomainException>(() => service.SetWorkingHoursAsync(2, hours.Take(6).ToList()));
        hours.Single(x => x.DayOfWeek == (int)Slot().AddHours(4).DayOfWeek).IsOpen = false;
        await service.SetWorkingHoursAsync(2, hours);
        Assert.Empty((await service.GetAvailabilityAsync(10, DateOnly.FromDateTime(Slot().AddHours(4)), 60)).Slots);
        Assert.Equal(AppointmentStatus.Accepted, (await context.Appointments.SingleAsync()).Status);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetWorkingHoursAsync(1));
        var day = DateOnly.FromDateTime(Slot().AddHours(4));
        Assert.Single(await service.GetCalendarAsync(2, day, day));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCalendarAsync(1, day, day));
        await Assert.ThrowsAsync<DomainException>(() => service.GetCalendarAsync(2, day, day.AddDays(31)));
    }
    [Fact]
    public async Task BookingUsesAvailabilityOnServerEvenIfClientBypassesSlotPicker()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var service = new AppointmentService(new AppointmentRepository(context), new MechanicRepository(context), new VehicleRepository(context),
            TestSupport.Mapper(), new TestUnitOfWork(context), new NotificationService(new NotificationRepository(context), TestSupport.Mapper()),
            new MotoGarage.Infrastructure.Authentication.AccountAccessService(context), TestSupport.Planning(context));
        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(3, new() { MechanicId = 10, VehicleId = 6, AppointmentDate = Slot().AddMinutes(15) }));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(3, new() { MechanicId = 10, VehicleId = 6, AppointmentDate = Slot().AddHours(10) }));
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(3, new() { MechanicId = 10, VehicleId = 6, AppointmentDate = Slot().AddMinutes(61) }));
        var created = await service.CreateAsync(3, new() { MechanicId = 10, VehicleId = 6, AppointmentDate = Slot().AddHours(1) });
        Assert.Equal(60, created.DurationMinutes); Assert.Equal("BMW 320", created.VehicleName);
    }
    [Fact]
    public async Task RescheduleRequiresOtherParticipantAndResetsEmailSchedule()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var a = await context.Appointments.FindAsync(20); a!.EmailReminderSentAtUtc = DateTime.UtcNow;
        a.EmailReminderLeaseId = Guid.NewGuid(); a.EmailReminderLeaseUntilUtc = DateTime.UtcNow.AddMinutes(5); a.EmailReminderNextAttemptAtUtc = DateTime.UtcNow.AddMinutes(2);
        await context.SaveChangesAsync(); var service = TestSupport.Planning(context); var next = Slot().AddHours(2);
        await service.ProposeAsync(20, 1, new() { AppointmentDate = next });
        Assert.Equal(Slot(), (await context.Appointments.SingleAsync()).AppointmentDate);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DecideRescheduleAsync(20, 1, new() { Approve = true, Revision = 1 }));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DecideRescheduleAsync(20, 3, new() { Approve = true, Revision = 1 }));
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideRescheduleAsync(20, 2, new() { Approve = true, Revision = 2 }));
        await service.DecideRescheduleAsync(20, 2, new() { Approve = true, Revision = 1 });
        context.ChangeTracker.Clear(); a = await context.Appointments.SingleAsync();
        Assert.Equal(next, a.AppointmentDate); Assert.Null(a.ProposedAppointmentDateUtc);
        Assert.Null(a.EmailReminderSentAtUtc); Assert.Null(a.EmailReminderLeaseId); Assert.Null(a.EmailReminderLeaseUntilUtc); Assert.Null(a.EmailReminderNextAttemptAtUtc);
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideRescheduleAsync(20, 2, new() { Approve = true, Revision = 1 }));
    }
    [Fact]
    public async Task ProposalDoesNotReserveNewSlotAndConfirmationRechecksConflict()
    {
        using var context = TestSupport.Context(); await Seed(context); var service = TestSupport.Planning(context); var next = Slot().AddHours(2);
        await service.ProposeAsync(20, 2, new() { AppointmentDate = next });
        context.Appointments.Add(new Appointment { UserId = 3, VehicleId = 6, MechanicId = 10, AppointmentDate = next, Status = AppointmentStatus.Pending });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideRescheduleAsync(20, 1, new() { Approve = true, Revision = 1 }));
        context.ChangeTracker.Clear(); Assert.Equal(Slot(), (await context.Appointments.FindAsync(20))!.AppointmentDate);
        await service.DecideRescheduleAsync(20, 2, new() { Approve = false, Revision = 1 });
        Assert.Null((await context.Appointments.FindAsync(20))!.ProposedAppointmentDateUtc);
    }
    [Fact]
    public async Task ReportNeedsCompletionAndOwnerApprovalBeforeAddingHistoryExactlyOnce()
    {
        using var context = TestSupport.Context(); await Seed(context); var service = TestSupport.Planning(context);
        await Assert.ThrowsAsync<ConflictException>(() => service.SubmitReportAsync(20, 2, Report()));
        await Complete(context);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.SubmitReportAsync(20, 1, Report()));
        await service.SubmitReportAsync(20, 2, Report());
        Assert.Empty(context.ServiceRecords); Assert.Equal(1000, (await context.Vehicles.FindAsync(5))!.CurrentKm);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetReportAsync(20, 3));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DecideReportAsync(20, 2, new() { Approve = true, Revision = 1 }));
        await service.DecideReportAsync(20, 1, new() { Approve = true, Revision = 1 });
        var record = Assert.Single(context.ServiceRecords); Assert.Equal("Oil filter", record.ReplacedParts); Assert.Equal(80, record.Cost);
        Assert.Equal(1200, (await context.Vehicles.FindAsync(5))!.CurrentKm);
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideReportAsync(20, 1, new() { Approve = true, Revision = 1 }));
        await Assert.ThrowsAsync<ConflictException>(() => service.SubmitReportAsync(20, 2, Report(1)));
        Assert.Single(context.ServiceRecords);
    }
    [Fact]
    public async Task RevisedReportRejectsStaleApprovalAndDoesNotReduceOdometer()
    {
        using var context = TestSupport.Context(); await Seed(context); await Complete(context); var service = TestSupport.Planning(context);
        await service.SubmitReportAsync(20, 2, Report());
        await Assert.ThrowsAsync<DomainException>(() => service.DecideReportAsync(20, 1, new() { Approve = false, Revision = 1 }));
        await service.DecideReportAsync(20, 1, new() { Approve = false, Revision = 1, Comment = "Correct parts" });
        Assert.Empty(context.ServiceRecords);
        var revised = Report(1); revised.Kilometer = 900;
        await service.SubmitReportAsync(20, 2, revised);
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideReportAsync(20, 1, new() { Approve = true, Revision = 1 }));
        await service.DecideReportAsync(20, 1, new() { Approve = true, Revision = 2 });
        Assert.Equal(1000, (await context.Vehicles.FindAsync(5))!.CurrentKm);
        Assert.Equal(900, Assert.Single(context.ServiceRecords).Kilometer);
    }
    [Fact]
    public async Task LowStockAlertsHaveOneNotificationPerEpisodeAndResetAfterRestock()
    {
        using var context = TestSupport.Context(); await Seed(context); var alerts = new StockAlertService(context);
        var product = await context.Products.SingleAsync();
        product.Stock = 5; await alerts.EvaluateAsync(product); await context.SaveChangesAsync();
        product.Stock = 1; await alerts.EvaluateAsync(product); await context.SaveChangesAsync();
        Assert.Single(context.Notifications); Assert.True(product.LowStockAlertActive);
        product.Stock = 9; await alerts.EvaluateAsync(product); await context.SaveChangesAsync(); Assert.False(product.LowStockAlertActive);
        product.Stock = 0; await alerts.EvaluateAsync(product); await context.SaveChangesAsync();
        Assert.Equal(2, await context.Notifications.CountAsync());
        Assert.Contains(await context.Notifications.ToListAsync(), x => x.Link == "/inventory?stock=out" && x.UserId == 4);
    }
    [Fact]
    public async Task ProductAndOrderServicesUseTheSameStockAlertPolicy()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var productService = new ProductService(new ProductRepository(context), new StoreRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context), new TestImages(), new StockAlertService(context));
        await productService.UpdateStockAsync("oil", 4, new() { Stock = 6 });
        await TestSupport.Orders(context).CreateAsync(1, new() { Items = [new() { ProductId = 40, Quantity = 1 }] });
        Assert.Single(await context.Notifications.Where(x => x.Type == NotificationType.General).ToListAsync());
        await productService.UpdateStockAsync("oil", 4, new() { Stock = 8 });
        await productService.UpdateAsync("oil", 4, new() { Name = "Oil", Price = 10, LowStockThreshold = 8 });
        Assert.Equal(2, await context.Notifications.CountAsync(x => x.Type == NotificationType.General));
        await productService.UpdateAsync("oil", 4, new() { Name = "New oil name", Price = 12 });
        Assert.Equal(8, (await context.Products.SingleAsync()).LowStockThreshold);
    }
    [Fact]
    public async Task StatisticsUseCompletedPricesAndOwnerScope()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var now = DateTime.UtcNow; var day = DateOnly.FromDateTime(now.AddHours(4));
        context.Orders.AddRange(new Order
        {
            Id = 50,
            UserId = 1,
            StoreId = 30,
            Status = OrderStatus.Completed,
            CreatedAt = now.AddMonths(-2),
            CompletedAtUtc = now,
            TotalAmount = 14,
            OrderItems = [new() { ProductId = 40, Quantity = 2, UnitPrice = 7, TotalPrice = 14 }]
        },
            new Order { UserId = 1, StoreId = 30, Status = OrderStatus.Pending, CreatedAt = now, TotalAmount = 100 },
            new Order { UserId = 1, StoreId = 30, Status = OrderStatus.Cancelled, CreatedAt = now.AddMonths(-2), CancelledAtUtc = now, TotalAmount = 100 });
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var service = new BusinessStatisticsService(context, new MotoGarage.Infrastructure.Authentication.AccountAccessService(context));
        var result = await service.GetAsync(4, day, day);
        Assert.Equal(14, result.Amount); Assert.Equal(1, result.CompletedCount); Assert.Equal(1, result.CancelledCount); Assert.Equal(1, result.PendingCount);
        Assert.Equal(2, Assert.Single(result.BestSellers).Quantity); Assert.Equal(14, result.BestSellers[0].Amount);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(3, day, day));
        await Complete(context); await TestSupport.Planning(context).SubmitReportAsync(20, 2, Report());
        Assert.Equal(0, (await service.GetAsync(2, day.AddDays(-1), day)).Amount);
        await TestSupport.Planning(context).DecideReportAsync(20, 1, new() { Approve = true, Revision = 1 });
        Assert.Equal(80, (await service.GetAsync(2, day.AddDays(-1), day)).Amount);
    }
    [Fact]
    public async Task AdminAuditRecordsActorAndDecisionAndSkipsRepeatedOperations()
    {
        using var context = TestSupport.Context(); await Seed(context); using var users = TestSupport.Users(context);
        var service = new AdminService(context, users, new TestUnitOfWork(context), new MotoGarage.API.Hubs.UserConnectionRegistry());
        await Assert.ThrowsAsync<ForbiddenException>(() => service.BlockUserAsync(3, 1));
        Assert.Empty(context.AdminAuditLogs);
        await service.BlockUserAsync(3, 99, "  Qaydaları pozduğu üçün  "); await service.BlockUserAsync(3, 99);
        await service.UnblockUserAsync(3, 99); await service.UnblockUserAsync(3, 99);
        await service.DeleteUserAsync(3, 99); await service.DeleteUserAsync(3, 99);
        var history = await new AdminAuditService(context).GetAsync(99, 1, 25, null);
        Assert.Equal(3, history.Total); Assert.All(history.Items, x => { Assert.Equal(99, x.AdminId); Assert.Equal("Admin", x.AdminName); Assert.Equal("Other", x.TargetName); Assert.False(string.IsNullOrWhiteSpace(x.Reason)); });
        Assert.Single((await new AdminAuditService(context).GetAsync(99, 1, 25, "UserDeleted")).Items);
        Assert.Equal("Qaydaları pozduğu üçün", Assert.Single((await new AdminAuditService(context).GetAsync(99, 1, 25, "UserBlocked")).Items).Reason);
        await Assert.ThrowsAsync<ForbiddenException>(() => new AdminAuditService(context).GetAsync(1, 1, 25, null));
    }

    [Fact]
    public async Task ActiveEmailLeasePreventsChangingTimeDuringSmtpDelivery()
    {
        using var context = TestSupport.Context(); await Seed(context);
        var service = TestSupport.Planning(context);
        await service.ProposeAsync(20, 1, new() { AppointmentDate = Slot().AddHours(2) });
        var a = await context.Appointments.FindAsync(20);
        a!.EmailReminderLeaseId = Guid.NewGuid(); a.EmailReminderLeaseUntilUtc = DateTime.UtcNow.AddMinutes(5);
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => service.DecideRescheduleAsync(20, 2, new() { Approve = true, Revision = 1 }));
        context.ChangeTracker.Clear(); Assert.Equal(Slot(), (await context.Appointments.FindAsync(20))!.AppointmentDate);
        a = await context.Appointments.FindAsync(20); a!.EmailReminderLeaseUntilUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        await service.DecideRescheduleAsync(20, 2, new() { Approve = true, Revision = 1 });
        Assert.Equal(Slot().AddHours(2), (await context.Appointments.FindAsync(20))!.AppointmentDate);
    }
}
