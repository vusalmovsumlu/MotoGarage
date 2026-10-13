using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Inventory;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Scheduling;

namespace MotoGarage.Tests;

public class BusinessOperationsSqlTests
{
    [LocalReminderSqlFact]
    public async Task MigrationConcurrentBookingsReportApprovalStockAlertsStatisticsAndRollbackUseRealSql()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server == "." || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) ||
            server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase) ||
            server == "127.0.0.1" || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
            server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_BusinessOperationsChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString,
            sql => { sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(300), [1205]); sql.CommandTimeout(120); })
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Error).Options;
        await using var database = new AppDbContext(options);
        AppointmentPlanningService Planning(AppDbContext c) => new(c, new UnitOfWork(c), new AccountAccessService(c), TestSupport.Mapper());
        AppointmentService Appointments(AppDbContext c) => new(new AppointmentRepository(c), new MechanicRepository(c), new VehicleRepository(c),
            TestSupport.Mapper(), new UnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), new AccountAccessService(c), Planning(c));
        OrderService Orders(AppDbContext c) => new(new OrderRepository(c), new ProductRepository(c), new StoreRepository(c), TestSupport.Mapper(),
            new UnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), new AccountAccessService(c), new StockAlertService(c));
        try
        {
            var previous = database.Database.GetMigrations().TakeWhile(x => !x.EndsWith("BusinessOperations")).Last();
            await database.GetService<IMigrator>().MigrateAsync(previous);
            await database.Database.ExecuteSqlRawAsync("""
                INSERT INTO [AspNetUsers] ([FullName], [Role], [CreatedAt], [EmailConfirmed], [IsAdminBlocked], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [BusinessApprovalStatus])
                VALUES (N'Legacy store', 3, SYSUTCDATETIME(), 1, 0, 0, 0, 0, 0, 2);
                INSERT INTO [Stores] ([UserId], [StoreName], [Slug], [Description], [PhoneNumber], [Address], [Rating], [IsActive], [CreatedAt])
                SELECT [Id], N'Legacy shop', 'legacy-shop', N'Parts', '123', N'Baku', 0, 1, SYSUTCDATETIME() FROM [AspNetUsers];
                INSERT INTO [Products] ([StoreId], [Name], [Slug], [Price], [Stock], [IsActive], [CreatedAt])
                SELECT [Id], N'Legacy product', 'legacy-product', 10, 8, 1, SYSUTCDATETIME() FROM [Stores];
                """);
            await database.Database.MigrateAsync();
            Assert.Equal(8, (await database.Products.SingleAsync()).Stock);
            Assert.Equal(5, (await database.Products.SingleAsync()).LowStockThreshold);
            Assert.Empty(database.AdminAuditLogs);
            var owner = new User { FullName = "Owner", EmailConfirmed = true };
            var other = new User { FullName = "Other", EmailConfirmed = true };
            var mechanicUser = new User { FullName = "Mechanic", Role = UserRole.Mechanic, EmailConfirmed = true };
            var seller = new User { FullName = "Seller", Role = UserRole.Store, EmailConfirmed = true };
            var mechanic = new Mechanic { User = mechanicUser, BusinessName = "Garage", Slug = "garage", Address = "Baku", Description = "Service", PhoneNumber = "123", IsActive = true };
            var vehicle = new Vehicle { User = owner, Brand = "Toyota", Model = "Corolla", Slug = "toyota", VIN = "VINONE", PlateNumber = "ONE", Engine = "1.8" };
            var secondVehicle = new Vehicle { User = other, Brand = "BMW", Model = "320", Slug = "bmw", VIN = "VINTWO", PlateNumber = "TWO", Engine = "2.0" };
            var store = new Store { User = seller, StoreName = "Shop", Slug = "shop", Address = "Baku", Description = "Parts", PhoneNumber = "123", IsActive = true };
            var product = new Product { Store = store, Name = "Oil", Slug = "oil", Price = 10, Stock = 6, LowStockThreshold = 5, IsActive = true };
            database.Vehicles.AddRange(vehicle, secondVehicle); database.Mechanics.Add(mechanic); database.Products.Add(product);
            await database.SaveChangesAsync(); database.ChangeTracker.Clear();
            var slot = BusinessOperationsTests.Slot();
            async Task<bool> Book(int userId, int vehicleId, DateTime date)
            {
                await using var attempt = new AppDbContext(options);
                try { await Appointments(attempt).CreateAsync(userId, new() { MechanicId = mechanic.Id, VehicleId = vehicleId, AppointmentDate = date }); return true; }
                catch (ConflictException) { return false; }
            }
            var bookings = await Task.WhenAll(Book(owner.Id, vehicle.Id, slot), Book(other.Id, secondVehicle.Id, slot.AddMinutes(15)));
            Assert.Single(bookings.Where(x => x));
            var appointment = await database.Appointments.SingleAsync();
            Assert.Single((await Planning(database).GetCalendarAsync(mechanicUser.Id, DateOnly.FromDateTime(slot.AddHours(4)), DateOnly.FromDateTime(slot.AddHours(4)))));
            var availability = await Planning(database).GetAvailabilityAsync(mechanic.Id, DateOnly.FromDateTime(slot.AddHours(4)), 60);
            Assert.DoesNotContain(appointment.AppointmentDate, availability.Slots);
            await Appointments(database).UpdateStatusAsync(appointment.Id, mechanicUser.Id, new() { Status = AppointmentStatus.Accepted });
            await Appointments(database).UpdateStatusAsync(appointment.Id, mechanicUser.Id, new() { Status = AppointmentStatus.Completed });
            await Planning(database).SubmitReportAsync(appointment.Id, mechanicUser.Id, new() { ServiceName = "Service", ReplacedParts = "Filter", Kilometer = 2000, Cost = 80, ServiceDate = DateTime.UtcNow.AddMinutes(-5) });
            async Task<bool> Approve()
            {
                await using var attempt = new AppDbContext(options);
                try { await Planning(attempt).DecideReportAsync(appointment.Id, appointment.UserId, new() { Approve = true, Revision = 1 }); return true; }
                catch (ConflictException) { return false; }
            }
            Assert.Single((await Task.WhenAll(Approve(), Approve())).Where(x => x));
            database.ChangeTracker.Clear(); Assert.Single(database.ServiceRecords);
            Assert.Equal("Filter", (await database.ServiceRecords.SingleAsync()).ReplacedParts);
            async Task Buy()
            {
                await using var attempt = new AppDbContext(options);
                await Orders(attempt).CreateAsync(owner.Id, TestSupport.Order(product.Id));
            }
            await Task.WhenAll(Buy(), Buy());
            database.ChangeTracker.Clear(); Assert.Equal(4, (await database.Products.SingleAsync(x => x.Id == product.Id)).Stock);
            Assert.Single(await database.Notifications.Where(x => x.UserId == seller.Id && x.Type == NotificationType.General).ToListAsync());
            foreach (var orderId in await database.Orders.Select(x => x.Id).ToListAsync())
                foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Completed })
                    await Orders(database).UpdateStatusAsync(orderId, seller.Id, new() { Status = status });
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(4));
            var stats = await new BusinessStatisticsService(database, new AccountAccessService(database)).GetAsync(seller.Id, today, today);
            Assert.Equal(20, stats.Amount); Assert.Equal(2, stats.CompletedCount); Assert.Equal(2, Assert.Single(stats.BestSellers).Quantity);
            var serviceStats = await new BusinessStatisticsService(database, new AccountAccessService(database)).GetAsync(mechanicUser.Id, today.AddDays(-1), today);
            Assert.Equal(80, serviceStats.Amount);
            var inventory = new ProductService(new ProductRepository(database), new StoreRepository(database), TestSupport.Mapper(), new UnitOfWork(database), new TestImages(), new StockAlertService(database));
            var zeroThreshold = await inventory.CreateAsync(seller.Id, new() { Name = "Only empty stock alert", Price = 1, Stock = 1, LowStockThreshold = 0 });
            database.ChangeTracker.Clear();
            Assert.Equal(0, (await database.Products.SingleAsync(x => x.Id == zeroThreshold.Id)).LowStockThreshold);
            await inventory.UpdateStockAsync("oil", seller.Id, new() { Stock = 8 });
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] WITH NOCHECK ADD CONSTRAINT [CK_Test_StockAlert] CHECK ([Title] <> N'Stok azalır');");
            await Assert.ThrowsAsync<ConflictException>(() => inventory.UpdateStockAsync("oil", seller.Id, new() { Stock = 5 }));
            database.ChangeTracker.Clear(); var unchanged = await database.Products.SingleAsync(x => x.Id == product.Id);
            Assert.Equal(8, unchanged.Stock); Assert.False(unchanged.LowStockAlertActive);
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Test_StockAlert];");
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (connection.InitialCatalog != databaseName || !databaseName.StartsWith("MotoGarage_BusinessOperationsChecks_")) throw new InvalidOperationException("Unsafe test cleanup target.");
            await database.Database.EnsureDeletedAsync();
        }
    }
}
