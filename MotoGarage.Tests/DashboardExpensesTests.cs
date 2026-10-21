using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class DashboardExpensesTests
{
    [Fact]
    public async Task MonthlyTotalIncludesFuelExpensesServicesAndCompletedOrdersForOnlyTheCurrentUserAndMonth()
    {
        await using var context = TestSupport.Context();
        await VerifyMonthlyTotalsAsync(context);
    }

    [LocalReminderSqlFact]
    public async Task MonthlyTotalAggregationWorksOnSqlServer()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use the isolated LocalDB test configuration.");

        var name = "MotoGarage_DashboardChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await context.Database.MigrateAsync();
            await VerifyMonthlyTotalsAsync(context);
        }
        finally
        {
            var target = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
            if (target.DataSource != connection.DataSource || target.InitialCatalog != name
                || !name.StartsWith("MotoGarage_DashboardChecks_"))
                throw new InvalidOperationException("Unsafe test database cleanup target.");
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static async Task VerifyMonthlyTotalsAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var owner = new User { FullName = "Owner", EmailConfirmed = true };
        var otherOwner = new User { FullName = "Other owner", EmailConfirmed = true };
        var car = Vehicle(owner, "CAR");
        var motorcycle = Vehicle(owner, "MOTO", VehicleType.Motorcycle);
        var otherCar = Vehicle(otherOwner, "OTHER");
        context.Vehicles.AddRange(car, motorcycle, otherCar);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var query = new DashboardQuery(context, TestSupport.Mapper());
        Assert.Equal(0m, (await query.GetAsync(owner.Id)).ThisMonthExpenses);

        // All seven equal-cost fills must count, even though only five recent
        // records are returned. Records on the first day are included.
        for (var day = 0; day < 7; day++)
            context.FuelRecords.Add(Fuel(car.Id, start.AddDays(day), 2.50m));
        context.FuelRecords.AddRange(
            Fuel(motorcycle.Id, start.AddDays(1), 12.50m),
            Fuel(car.Id, start.AddTicks(-1), 500m),
            Fuel(car.Id, end, 500m),
            Fuel(otherCar.Id, start, 1000m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var fuelOnly = await query.GetAsync(owner.Id);
        Assert.Equal(30m, fuelOnly.ThisMonthExpenses);
        Assert.Equal(5, fuelOnly.RecentFuelRecords.Count());
        Assert.Empty(context.ChangeTracker.Entries());

        context.Expenses.AddRange(
            Expense(car.Id, start, 7.25m, ExpenseType.Fuel),
            Expense(motorcycle.Id, start.AddDays(1), 9.75m, ExpenseType.Insurance),
            Expense(car.Id, start.AddTicks(-1), 500m),
            Expense(car.Id, end, 500m),
            Expense(otherCar.Id, start, 600m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(47m, (await query.GetAsync(owner.Id)).ThisMonthExpenses);
        Assert.Equal(1600m, (await query.GetAsync(otherOwner.Id)).ThisMonthExpenses);
        Assert.Empty(context.ChangeTracker.Entries());

        context.ServiceRecords.AddRange(
            Service(car.Id, start, 20m), Service(motorcycle.Id, start.AddDays(1), 20m),
            Service(car.Id, start.AddTicks(-1), 500m), Service(car.Id, end, 500m),
            Service(otherCar.Id, start, 30m));
        var store = new Store
        {
            User = new User { FullName = "Seller", Role = UserRole.Store }, StoreName = "Parts",
            Slug = "parts", Description = "Parts", Address = "Baku", PhoneNumber = "123"
        };
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        Order Order(int buyer, OrderStatus status, DateTime created, DateTime? completed, decimal amount) => new()
        {
            UserId = buyer, StoreId = store.Id, Status = status,
            CreatedAt = created, CompletedAtUtc = completed, TotalAmount = amount
        };
        context.Orders.AddRange(
            Order(owner.Id, OrderStatus.Completed, start.AddMonths(-1), start, 40m),
            Order(owner.Id, OrderStatus.Completed, start, null, 10m), // Legacy completed order.
            Order(owner.Id, OrderStatus.Completed, start, start.AddTicks(-1), 500m),
            Order(owner.Id, OrderStatus.Completed, start, end, 500m),
            Order(otherOwner.Id, OrderStatus.Completed, start, start, 25m));
        foreach (var status in Enum.GetValues<OrderStatus>().Where(x => x != OrderStatus.Completed))
            context.Orders.Add(Order(owner.Id, status, start, start, 500m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(137m, (await query.GetAsync(owner.Id)).ThisMonthExpenses);
        Assert.Equal(1655m, (await query.GetAsync(otherOwner.Id)).ThisMonthExpenses);
        Assert.Empty(context.ChangeTracker.Entries());

        var service = await context.ServiceRecords.SingleAsync(x => x.VehicleId == car.Id && x.ServiceDate == start);
        service.Cost = 30m;
        await context.SaveChangesAsync();
        Assert.Equal(147m, (await query.GetAsync(owner.Id)).ThisMonthExpenses);
        context.ServiceRecords.Remove(service);
        await context.SaveChangesAsync();
        Assert.Equal(117m, (await query.GetAsync(owner.Id)).ThisMonthExpenses);
    }

    private static Vehicle Vehicle(User owner, string plate, VehicleType type = VehicleType.Car) => new()
    {
        User = owner, Brand = "Honda", Model = plate, Year = 2020, Engine = "500",
        VIN = "VIN-" + plate, PlateNumber = plate, VehicleType = type,
        FuelType = FuelType.Petrol
    };

    private static FuelRecord Fuel(int vehicleId, DateTime date, decimal amount) => new()
    {
        VehicleId = vehicleId, FuelDate = date, Liters = 5m,
        PricePerLiter = amount / 5m, TotalPrice = amount, Kilometer = 100
    };

    private static Expense Expense(int vehicleId, DateTime date, decimal amount,
        ExpenseType type = ExpenseType.Other) => new()
    {
        VehicleId = vehicleId, ExpenseDate = date, Amount = amount,
        ExpenseType = type, Description = "Test expense"
    };

    private static ServiceRecord Service(int vehicleId, DateTime date, decimal amount) => new()
    {
        VehicleId = vehicleId, ServiceDate = date, Cost = amount,
        ServiceName = "Oil change", Kilometer = 100
    };
}
