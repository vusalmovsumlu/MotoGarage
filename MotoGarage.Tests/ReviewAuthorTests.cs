using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class ReviewAuthorTests
{
    [Fact]
    public async Task ExistingReviewAndDetailReturnAuthorNameEvenAfterTrackerIsCleared()
    {
        using var context = TestSupport.Context();
        context.Users.AddRange(new User { Id = 1, FullName = "Vüsal Mövsümlü" }, new User { Id = 2, FullName = "Başqa istifadəçi" });
        context.Mechanics.Add(new Mechanic { Id = 10, UserId = 2, BusinessName = "Servis", Description = "Servis", Address = "Bakı", PhoneNumber = "123", Slug = "servis", IsActive = true });
        context.Reviews.Add(new Review { Id = 20, UserId = 1, MechanicId = 10, Rating = 5, Comment = "Yaxşı xidmət" });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new ReviewService(new ReviewRepository(context), new MechanicRepository(context), new AppointmentRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context));
        var list = await service.GetByMechanicIdAsync(10);
        Assert.Equal("Vüsal Mövsümlü", Assert.Single(list).UserFullName);
        context.ChangeTracker.Clear();
        var detail = await service.GetByIdAsync(20);
        Assert.Equal("Vüsal Mövsümlü", detail.UserFullName);
        Assert.Equal(1, detail.UserId);

        var author = await context.Users.SingleAsync(x => x.Id == 1);
        author.FullName = "Vüsal Yeni Soyad";
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Equal("Vüsal Yeni Soyad", (await service.GetByIdAsync(20)).UserFullName);
    }

    [Fact]
    public async Task CreateReturnsAuthenticatedAuthorsNameAndKeepsReviewOwnership()
    {
        using var context = TestSupport.Context();
        context.Users.AddRange(new User { Id = 1, FullName = "Vüsal Mövsümlü" }, new User { Id = 2, FullName = "Usta" });
        context.Mechanics.Add(new Mechanic { Id = 10, UserId = 2, BusinessName = "Servis", Description = "Servis", Address = "Bakı", PhoneNumber = "123", Slug = "servis", IsActive = true });
        context.Vehicles.Add(new Vehicle { Id = 5, UserId = 1, Brand = "Toyota", Model = "Corolla", Slug = "toyota", Engine = "1.8", VIN = "TESTVIN", PlateNumber = "TEST" });
        context.Appointments.Add(new Appointment { UserId = 1, MechanicId = 10, VehicleId = 5, Status = AppointmentStatus.Completed });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new ReviewService(new ReviewRepository(context), new MechanicRepository(context), new AppointmentRepository(context), TestSupport.Mapper(), new TestUnitOfWork(context));
        var created = await service.CreateAsync(1, new() { MechanicId = 10, Rating = 4, Comment = "Təşəkkürlər" });
        Assert.Equal("Vüsal Mövsümlü", created.UserFullName);
        Assert.Equal(1, created.UserId);
        Assert.Equal(4, (await context.Mechanics.SingleAsync()).Rating);
        await Assert.ThrowsAsync<MotoGarage.Domain.Exceptions.ForbiddenException>(() => service.UpdateAsync(created.Id, 2, new() { Rating = 1, Comment = "Unauthorized" }));
    }
}
