using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.DTOs.Dashboard;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Infrastructure.Data;

public class DashboardQuery : IDashboardQuery
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public DashboardQuery(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<DashboardDto> GetAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var monthlyAmounts = _context.Expenses
            .Where(x => x.Vehicle.UserId == userId && x.ExpenseDate >= start && x.ExpenseDate < end)
            .Select(x => (decimal?)x.Amount)
            .Concat(_context.FuelRecords
                .Where(x => x.Vehicle.UserId == userId && x.FuelDate >= start && x.FuelDate < end)
                .Select(x => (decimal?)x.TotalPrice))
            .Concat(_context.ServiceRecords
                .Where(x => x.Vehicle.UserId == userId && x.ServiceDate >= start && x.ServiceDate < end)
                .Select(x => (decimal?)x.Cost))
            .Concat(_context.Orders
                .Where(x => x.UserId == userId && x.Status == OrderStatus.Completed
                    && (x.CompletedAtUtc ?? x.CreatedAt) >= start && (x.CompletedAtUtc ?? x.CreatedAt) < end)
                .Select(x => (decimal?)x.TotalAmount));
        var reminders = _context.Reminders.AsNoTracking()
            .Where(x => x.Vehicle.UserId == userId && !x.IsCompleted);
        var appointments = _context.Appointments.AsNoTracking().Where(x => x.UserId == userId);
        return new DashboardDto
        {
            TotalVehicles = await _context.Vehicles.CountAsync(x => x.UserId == userId),
            ThisMonthExpenses = await monthlyAmounts.SumAsync() ?? 0m,
            ActiveRemindersCount = await reminders.CountAsync(),
            PendingAppointmentsCount = await appointments.CountAsync(x => x.Status == AppointmentStatus.Pending),
            UpcomingReminders = await reminders.OrderBy(x => x.ReminderDate).ThenBy(x => x.Id).Take(5)
                .ProjectTo<ReminderDto>(_mapper.ConfigurationProvider).ToListAsync(),
            RecentFuelRecords = await _context.FuelRecords.AsNoTracking().Where(x => x.Vehicle.UserId == userId)
                .OrderByDescending(x => x.FuelDate).ThenByDescending(x => x.Id).Take(5)
                .ProjectTo<FuelRecordDto>(_mapper.ConfigurationProvider).ToListAsync(),
            UpcomingAppointments = await appointments.Where(x => x.AppointmentDate >= now &&
                (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted))
                .OrderBy(x => x.AppointmentDate).ThenBy(x => x.Id).Take(5)
                .ProjectTo<AppointmentDto>(_mapper.ConfigurationProvider).ToListAsync()
        };
    }
}
