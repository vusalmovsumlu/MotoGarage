using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.API.Hubs;
using Microsoft.Extensions.Configuration;
using MotoGarage.Infrastructure.Authentication;

namespace MotoGarage.Tests;

public class RulesAndSchemaTests
{
    [Theory]
    [InlineData("my-profile")]
    [InlineData("search")]
    [InlineData("create")]
    [InlineData("update")]
    public void ReservedSlugsCannotShadowRoutes(string name) => Assert.NotEqual(name, SlugHelper.Generate(name));

    [Fact]
    public void AppointmentTerminalStatesCannotBeReopened()
    {
        foreach (var terminal in new[] { AppointmentStatus.Rejected, AppointmentStatus.Completed, AppointmentStatus.Cancelled })
            foreach (var next in Enum.GetValues<AppointmentStatus>()) Assert.False(StatusTransitions.CanChange(terminal, next));
        Assert.False(StatusTransitions.CanChange(AppointmentStatus.Pending, AppointmentStatus.Completed));
        Assert.True(StatusTransitions.CanChange(AppointmentStatus.Accepted, AppointmentStatus.Completed));
    }

    [Fact]
    public void ShippedOrderCannotBeCancelledOrMoveBackwards()
    {
        Assert.False(StatusTransitions.CanChange(OrderStatus.Shipped, OrderStatus.Cancelled));
        Assert.False(StatusTransitions.CanChange(OrderStatus.Confirmed, OrderStatus.Pending));
        Assert.True(StatusTransitions.CanChange(OrderStatus.Preparing, OrderStatus.Cancelled));
    }

    [Fact]
    public void EndDateIncludesTheEntireCalendarDay()
    {
        var range = DateRange.Days(new(2026, 10, 1), new(2026, 10, 7));
        Assert.Equal(new DateTime(2026, 10, 8), range.EndExclusive);
        Assert.True(new DateTime(2026, 10, 7, 23, 59, 59) < range.EndExclusive);
        Assert.Throws<DomainException>(() => DateRange.Days(new(2026, 10, 8), new(2026, 10, 7)));
        Assert.Throws<DomainException>(() => DateRange.Days(new(2026, 10, 7, 18, 0, 0), new(2026, 10, 7, 9, 0, 0)));
    }

    [Fact]
    public void ValidationRejectsNullOrderItemsAndHugeFuelValues()
    {
        Assert.Throws<DomainException>(() => DtoValidation.Validate(new CreateOrderDto { Items = new() { null! } }));
        Assert.Throws<DomainException>(() => DtoValidation.Validate(new CreateOrderDto()));
        Assert.Throws<DomainException>(() => DtoValidation.Validate(new CreateOrderDto { Items = new() { new() { Quantity = 0 } } }));
        Assert.Throws<DomainException>(() => DtoValidation.Validate(new CreateFuelRecordDto { Liters = decimal.MaxValue, PricePerLiter = 1 }));
    }

    [Fact]
    public void BlockingDisconnectsOnlyTheTargetUsersLiveConnections()
    {
        var registry = new UserConnectionRegistry();
        var count = 0;
        registry.Register(1, "one", () => count++);
        registry.Register(1, "two", () => count++);
        registry.Register(2, "other", () => count += 100);
        registry.Disconnect(1);
        registry.Disconnect(1);
        Assert.Equal(2, count);
    }

    [Fact]
    public void StartupRejectsMissingAndShortJwtKeyWithoutRevealingTheKey()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "short" }).Build();
        var error = Assert.Throws<InvalidOperationException>(() => StartupConfiguration.Validate(config));
        Assert.Contains("32", error.Message);
        Assert.DoesNotContain("short", error.Message);
    }

    [Fact]
    public void SqlServerModelProtectsConcurrencyUniquenessAndConversationTargets()
    {
        using var context = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
        foreach (var type in new[] { typeof(Product), typeof(Order), typeof(Appointment), typeof(Vehicle), typeof(RefreshToken) })
        {
            var version = context.Model.FindEntityType(type)!.FindProperty("RowVersion")!;
            Assert.True(version.IsConcurrencyToken);
            Assert.Equal("rowversion", version.GetColumnType());
        }
        var review = context.Model.FindEntityType(typeof(Review))!;
        Assert.Contains(review.GetIndexes(), index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(new[] { "UserId", "MechanicId" }));
        var slot = context.Model.FindEntityType(typeof(Appointment))!.GetIndexes().Single(x => x.GetDatabaseName() == "UX_Appointments_ActiveSlot");
        Assert.True(slot.IsUnique);
        Assert.Equal("[Status] IN (1, 2)", slot.GetFilter());
    }

    [Fact]
    public void UpgradeSqlHashesLegacyTokensAndDoesNotTruncateExistingUrls()
    {
        using var context = new AppDbContextFactory().CreateDbContext(Array.Empty<string>());
        var script = context.GetService<IMigrator>().GenerateScript("20261007165317_RenameMileageToKm", "SecurityAndConcurrencyImprovements");
        Assert.Contains("HASHBYTES('SHA2_256'", script);
        Assert.Contains("rowversion", script);
        Assert.Contains("Duplicate reviews", script);
        Assert.DoesNotContain("ALTER COLUMN [ImageUrl]", script);
    }
}
