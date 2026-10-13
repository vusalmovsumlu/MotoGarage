using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class AppointmentNamesTests
{
    static async Task Seed(AppDbContext context)
    {
        context.Users.AddRange(new User { Id = 1, FullName = "Vüsal Mövsümlü" }, new User { Id = 2, FullName = "Əli Məmmədov" }, new User { Id = 3, FullName = "Başqa istifadəçi" });
        context.Mechanics.Add(new Mechanic { Id = 10, UserId = 2, BusinessName = "Servis adı", Description = "Servis", Address = "Bakı", PhoneNumber = "123", Slug = "servis", IsActive = true });
        context.Vehicles.Add(new Vehicle { Id = 5, UserId = 1, Brand = "Toyota", Model = "Corolla", Slug = "toyota", Engine = "1.8", VIN = "TESTVIN", PlateNumber = "TEST" });
        context.Appointments.Add(new Appointment { Id = 20, UserId = 1, MechanicId = 10, VehicleId = 5, Status = AppointmentStatus.Pending, AppointmentDate = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    static DateTime NextSlot() { var day = DateTime.UtcNow.AddHours(4).Date.AddDays(2); if (day.DayOfWeek == DayOfWeek.Sunday) day = day.AddDays(1); return DateTime.SpecifyKind(day.AddHours(6), DateTimeKind.Utc); }

    static AppointmentService Service(AppDbContext context) => new(
        new AppointmentRepository(context), new MechanicRepository(context), new VehicleRepository(context),
        TestSupport.Mapper(), new TestUnitOfWork(context), new NotificationService(new NotificationRepository(context), TestSupport.Mapper()), new MotoGarage.Infrastructure.Authentication.AccountAccessService(context), TestSupport.Planning(context));

    [Fact]
    public async Task ExistingAppointmentsReturnRealNamesInBothListsDetailAndDashboardWithoutTrackerFixup()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var service = Service(context);
        var mechanics = new MechanicService(new MechanicRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context), new TestImages());
        var selection = Assert.Single(await mechanics.GetAllAsync());
        Assert.Equal("Servis adı", selection.BusinessName);
        Assert.Equal("Əli Məmmədov", selection.FullName);
        Assert.Equal("Əli Məmmədov", Assert.Single(await mechanics.SearchAsync("Servis", null)).FullName);
        var outgoing = Assert.Single(await service.GetMyAppointmentsAsync(1));
        Assert.Equal("Toyota Corolla", outgoing.VehicleName);
        Assert.Equal("Əli Məmmədov", outgoing.MechanicName);
        Assert.Equal("Vüsal Mövsümlü", outgoing.CustomerName);
        context.ChangeTracker.Clear();
        var incoming = Assert.Single(await service.GetMechanicAppointmentsAsync(2));
        Assert.Equal("Toyota Corolla", incoming.VehicleName);
        Assert.Equal("Vüsal Mövsümlü", incoming.CustomerName);
        Assert.Equal("Əli Məmmədov", incoming.MechanicName);
        context.ChangeTracker.Clear();
        Assert.Equal("Əli Məmmədov", (await service.GetByIdAsync(20, 1)).MechanicName);
        context.ChangeTracker.Clear();
        var dashboard = await new DashboardQuery(context, TestSupport.Mapper()).GetAsync(1);
        Assert.Equal("Əli Məmmədov", Assert.Single(dashboard.UpcomingAppointments).MechanicName);
        Assert.Equal("Toyota Corolla", Assert.Single(dashboard.UpcomingAppointments).VehicleName);
        Assert.Empty(await service.GetMyAppointmentsAsync(3));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(20, 3));
    }

    [Fact]
    public async Task NewlyCreatedAppointmentReturnsNamesAndMissingPersonalNameFallsBackToBusinessName()
    {
        using var context = TestSupport.Context();
        await Seed(context);
        var service = Service(context);
        var created = await service.CreateAsync(1, new() { MechanicId = 10, VehicleId = 5, AppointmentDate = NextSlot(), Description = "Yağ dəyişimi" });
        Assert.Equal("Toyota Corolla", created.VehicleName);
        Assert.Equal("Əli Məmmədov", created.MechanicName);
        Assert.Equal("Vüsal Mövsümlü", created.CustomerName);
        var mechanicUser = await context.Users.FindAsync(2);
        mechanicUser!.FullName = "";
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.All(await service.GetMyAppointmentsAsync(1), item => Assert.Equal("Servis adı", item.MechanicName));
    }
}
