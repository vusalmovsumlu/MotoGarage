using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class VehicleSlugTests
{
    [Fact]
    public async Task SameModelGetsUniqueStableSlugsAndOnlyOwnerCanReadThem()
    {
        using var context = TestSupport.Context();
        context.Users.AddRange(new User { Id = 1, FullName = "Owner" }, new User { Id = 2, FullName = "Other" });
        await context.SaveChangesAsync();
        var service = new VehicleService(new VehicleRepository(context), TestSupport.Mapper(),
            new TestUnitOfWork(context), new AppointmentRepository(context), new TestImages());
        CreateVehicleDto Create(string vin) => new()
        {
            Brand = "Toyota", Model = "Corolla", Year = 2022, Engine = "1.8",
            VIN = vin, PlateNumber = "10-AB-123", CurrentKm = 100,
            VehicleType = VehicleType.Car, FuelType = FuelType.Petrol
        };
        var first = await service.CreateAsync(1, Create("VIN-ONE"));
        var second = await service.CreateAsync(1, Create("VIN-TWO"));
        Assert.Matches("^toyota-corolla-[a-f0-9]{32}$", first.Slug);
        Assert.NotEqual(first.Slug, second.Slug);
        Assert.Equal(first.Id, (await service.GetBySlugAsync(first.Slug, 1)).Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBySlugAsync(first.Slug, 2));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetBySlugAsync("missing-slug", 1));
        await service.UpdateAsync(first.Id, 1, new UpdateVehicleDto
        {
            Brand = "New brand", Model = "New model", Year = 2022, Engine = "1.8",
            VIN = "VIN-ONE", PlateNumber = "10-AB-123", CurrentKm = 100,
            VehicleType = VehicleType.Car, FuelType = FuelType.Petrol
        });
        Assert.Equal("New model", (await service.GetBySlugAsync(first.Slug, 1)).Model);
        Assert.Equal(first.Slug, (await service.GetByIdAsync(first.Id)).Slug);
    }

    [Fact]
    public void SlugHasUniqueRequiredBoundedIndex()
    {
        using var context = TestSupport.Context();
        var entity = context.Model.FindEntityType(typeof(Vehicle))!;
        Assert.False(entity.FindProperty(nameof(Vehicle.Slug))!.IsNullable);
        Assert.Equal(200, entity.FindProperty(nameof(Vehicle.Slug))!.GetMaxLength());
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Vehicle.Slug));
    }

    [LocalReminderSqlFact]
    public async Task MigrationBackfillsExistingVehiclesWithoutLosingData()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_SlugChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        if (new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog != databaseName)
            throw new InvalidOperationException("Unexpected test database.");
        try
        {
            // The first migration pass also covers the empty database case.
            await context.Database.MigrateAsync();
            var owner = new User { FullName = "Slug migration test" };
            var names = new[] { ("Toyota", "Corolla"), ("Toyota", "Corolla"), ("ƏÖÜĞŞÇ", "450 / SR!"), ("???", "!!!") };
            context.Vehicles.AddRange(names.Select((name, index) => new Vehicle
            {
                User = owner, Brand = name.Item1, Model = name.Item2, Year = 2022,
                Engine = "1.8", VIN = $"TEST-VIN-{index}", PlateNumber = $"TEST-{index}", CurrentKm = 100
            }));
            await context.SaveChangesAsync();
            var before = await context.Vehicles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.VIN, x.CurrentKm }).ToListAsync();
            context.ChangeTracker.Clear();
            await context.GetService<IMigrator>().MigrateAsync("20261008174618_AddAutomaticReminderNotifications");
            await context.Database.MigrateAsync();
            var vehicles = await context.Vehicles.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            Assert.Equal(before, vehicles.Select(x => new { x.Id, x.VIN, x.CurrentKm }).ToList());
            Assert.Equal(4, vehicles.Select(x => x.Slug).Distinct().Count());
            Assert.All(vehicles, vehicle => Assert.Matches("^[a-z0-9-]+-[a-f0-9]{32}$", vehicle.Slug));
            Assert.StartsWith("eougsc-450-sr-", vehicles[2].Slug);
            Assert.StartsWith("qaraj-", vehicles[3].Slug);
            await context.Database.MigrateAsync();
            Assert.Equal(vehicles.Select(x => x.Slug), await context.Vehicles.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Slug).ToListAsync());
            await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Vehicles] SET [Slug] = {vehicles[0].Slug} WHERE [Id] = {vehicles[1].Id}"));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
