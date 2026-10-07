using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class VehicleAndQueryTests
{
    [Fact]
    public async Task FuelAndServiceRecordsRaiseOdometerAndCorrectionsNeverLowerIt()
    {
        using var context = TestSupport.Context();
        context.Users.Add(new User { Id = 1, FullName = "User", EmailConfirmed = true });
        context.Vehicles.Add(Vehicle());
        await context.SaveChangesAsync();
        var vehicles = new VehicleRepository(context);
        var fuel = new FuelRecordService(new FuelRecordRepository(context), vehicles, TestSupport.Mapper(), new TestUnitOfWork(context));
        await fuel.CreateAsync(1, new() { VehicleId = 1, Kilometer = 200, Liters = 20, PricePerLiter = 1, FuelDate = DateTime.UtcNow });
        var service = new ServiceRecordService(new ServiceRecordRepository(context), vehicles, TestSupport.Mapper(), new TestUnitOfWork(context));
        var record = await service.CreateAsync(1, new() { VehicleId = 1, Kilometer = 300, ServiceName = "Service", Cost = 10, ServiceDate = DateTime.UtcNow });
        Assert.Equal(300, (await context.Vehicles.SingleAsync()).CurrentKm);
        await service.UpdateAsync(record.Id, 1, new() { Kilometer = 150, ServiceName = "Correction", Cost = 10 });
        Assert.Equal(300, (await context.Vehicles.SingleAsync()).CurrentKm);
        var vehicleService = new VehicleService(vehicles, TestSupport.Mapper(), new TestUnitOfWork(context), new AppointmentRepository(context), new TestImages());
        await Assert.ThrowsAsync<DomainException>(() => vehicleService.UpdateAsync(1, 1, UpdateVehicle(100)));
    }

    [Fact]
    public async Task VehicleWithAppointmentReturnsConflictBeforeDelete()
    {
        using var context = TestSupport.Context();
        context.Users.Add(new User { Id = 1, FullName = "User" });
        context.Vehicles.Add(Vehicle());
        context.Appointments.Add(new Appointment { UserId = 1, MechanicId = 1, VehicleId = 1, Status = AppointmentStatus.Rejected });
        await context.SaveChangesAsync();
        var service = new VehicleService(new VehicleRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context), new AppointmentRepository(context), new TestImages());
        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(1, 1));
        Assert.Single(context.Vehicles);
    }

    [Fact]
    public async Task TrackedUpdateDoesNotMarkUnchangedStockModified()
    {
        using var context = TestSupport.Context();
        await TestSupport.SeedOrderData(context);
        var repository = new ProductRepository(context);
        var product = await repository.GetByIdAsync(1);
        product!.Name = "New name";
        await repository.UpdateAsync(product);
        context.ChangeTracker.DetectChanges();
        Assert.True(context.Entry(product).Property(x => x.Name).IsModified);
        Assert.False(context.Entry(product).Property(x => x.Stock).IsModified);
    }

    [Fact]
    public async Task ChatParticipantQueryDoesNotLoadHistory()
    {
        using var context = TestSupport.Context();
        await TestSupport.SeedOrderData(context);
        context.Conversations.Add(new Conversation { Id = 1, UserId = 1, StoreId = 1 });
        context.Messages.Add(new Message { ConversationId = 1, SenderUserId = 1, Content = "Message" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var conversation = await new ConversationRepository(context).GetParticipantsAsync(1);
        Assert.NotNull(conversation!.Store);
        Assert.Empty(context.ChangeTracker.Entries<Message>());
        Assert.False(context.Entry(conversation).Collection(x => x.Messages).IsLoaded);
    }

    [Fact]
    public async Task DashboardSelectsFiveRecordsWithoutTrackingEntities()
    {
        using var context = TestSupport.Context();
        context.Users.Add(new User { Id = 1, FullName = "User" });
        context.Vehicles.Add(Vehicle());
        for (var index = 1; index <= 12; index++)
            context.FuelRecords.Add(new FuelRecord { VehicleId = 1, Kilometer = index * 100, Liters = 10, PricePerLiter = 1, TotalPrice = 10, FuelDate = DateTime.UtcNow.AddDays(-index) });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var result = await new DashboardQuery(context, TestSupport.Mapper()).GetAsync(1);
        Assert.Equal(1, result.TotalVehicles);
        Assert.Equal(5, result.RecentFuelRecords.Count());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    private static Vehicle Vehicle() => new() { Id = 1, UserId = 1, Brand = "Honda", Model = "CB", Engine = "500", VIN = "VIN", PlateNumber = "PLATE", Year = 2020, CurrentKm = 100, VehicleType = VehicleType.Motorcycle, FuelType = FuelType.Petrol };
    private static UpdateVehicleDto UpdateVehicle(int km) => new() { Brand = "Honda", Model = "CB", Engine = "500", VIN = "VIN", PlateNumber = "PLATE", Year = 2020, CurrentKm = km, VehicleType = VehicleType.Motorcycle, FuelType = FuelType.Petrol };
}
