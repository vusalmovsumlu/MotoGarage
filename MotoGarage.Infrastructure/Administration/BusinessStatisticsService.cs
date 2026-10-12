using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Business;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Scheduling;

namespace MotoGarage.Infrastructure.Administration;

public sealed class BusinessStatisticsService(AppDbContext context, IAccountAccessService accounts) : IBusinessStatisticsService
{
    public async Task<BusinessStatisticsDto> GetAsync(int userId, DateOnly from, DateOnly to)
    {
        await accounts.RequireParticipantAsync(userId);
        if (from.Year < 1900 || to == DateOnly.MaxValue || to < from || to.DayNumber - from.DayNumber >= 366) throw new DomainException("Dövr maksimum 366 gün ola bilər.");
        var start = AppointmentPlanningService.StartOfDay(from); var end = AppointmentPlanningService.StartOfDay(to.AddDays(1));
        var mechanic = await context.Mechanics.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        if (mechanic is not null)
        {
            var entries = context.Appointments.AsNoTracking().Where(x => x.MechanicId == mechanic.Id);
            var completed = await entries.CountAsync(x => x.Status == AppointmentStatus.Completed &&
                (x.CompletedAtUtc ?? x.AppointmentDate) >= start && (x.CompletedAtUtc ?? x.AppointmentDate) < end);
            var pending = await entries.CountAsync(x => x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted);
            var reviews = context.Reviews.Where(x => x.MechanicId == mechanic.Id);
            var reviewCount = await reviews.CountAsync();
            var average = await reviews.Select(x => (double?)x.Rating).AverageAsync() ?? 0;
            var amount = await context.AppointmentServiceReports.Where(x => x.Appointment.MechanicId == mechanic.Id && x.Status == "Approved" &&
                x.ServiceDate >= start && x.ServiceDate < end).Select(x => (decimal?)x.Cost).SumAsync() ?? 0;
            return new("Mechanic", start, end, completed, 0, pending, amount, average, reviewCount, 0, 0, []);
        }
        var store = await context.Stores.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive)
            ?? throw new ForbiddenException("Aktiv biznes profili lazımdır.");
        var orders = context.Orders.AsNoTracking().Where(x => x.StoreId == store.Id);
        var completedOrders = orders.Where(x => x.Status == OrderStatus.Completed &&
            (x.CompletedAtUtc ?? x.CreatedAt) >= start && (x.CompletedAtUtc ?? x.CreatedAt) < end);
        var count = await completedOrders.CountAsync();
        var revenue = await completedOrders.Select(x => (decimal?)x.TotalAmount).SumAsync() ?? 0;
        var cancelled = await orders.CountAsync(x => x.Status == OrderStatus.Cancelled &&
            (x.CancelledAtUtc ?? x.CreatedAt) >= start && (x.CancelledAtUtc ?? x.CreatedAt) < end);
        var open = await orders.CountAsync(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled);
        var products = context.Products.AsNoTracking().Where(x => x.StoreId == store.Id && x.IsActive);
        var low = await products.CountAsync(x => x.Stock > 0 && x.Stock <= x.LowStockThreshold);
        var empty = await products.CountAsync(x => x.Stock == 0);
        var best = await context.OrderItems.AsNoTracking().Where(x => completedOrders.Select(o => o.Id).Contains(x.OrderId))
            .GroupBy(x => new { x.ProductId, x.Product.Name })
            .Select(g => new { g.Key.ProductId, g.Key.Name, Quantity = g.Sum(x => x.Quantity), Amount = g.Sum(x => x.TotalPrice) })
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.ProductId).Take(10).ToListAsync();
        return new("Store", start, end, count, cancelled, open, revenue, 0, 0, low, empty,
            best.Select(x => new PopularProductDto(x.ProductId, x.Name, x.Quantity, x.Amount)).ToList());
    }
}
