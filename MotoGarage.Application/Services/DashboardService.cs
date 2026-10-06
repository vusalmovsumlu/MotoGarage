using AutoMapper;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.DTOs.Dashboard;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IReminderRepository _reminderRepository;
    private readonly IFuelRecordRepository _fuelRecordRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public DashboardService(
        IVehicleRepository vehicleRepository,
        IExpenseRepository expenseRepository,
        IReminderRepository reminderRepository,
        IFuelRecordRepository fuelRecordRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _expenseRepository = expenseRepository;
        _reminderRepository = reminderRepository;
        _fuelRecordRepository = fuelRecordRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        int userId)
    {
        var vehicles =
            (await _vehicleRepository
                .GetByUserIdAsync(userId))
            .ToList();

        decimal thisMonthExpenses = 0;

        var allReminders =
            new List<Reminder>();

        var allFuelRecords =
            new List<FuelRecord>();

        var monthStart =
            new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1);

        var monthEnd =
            monthStart
                .AddMonths(1)
                .AddTicks(-1);

        foreach (var vehicle in vehicles)
        {
            var expenses =
                await _expenseRepository
                    .GetByDateRangeAsync(
                        vehicle.Id,
                        monthStart,
                        monthEnd);

            thisMonthExpenses +=
                expenses.Sum(x => x.Amount);

            var reminders =
                await _reminderRepository
                    .GetActiveRemindersAsync(
                        vehicle.Id);

            allReminders.AddRange(reminders);

            var fuelRecords =
                await _fuelRecordRepository
                    .GetByVehicleIdAsync(
                        vehicle.Id);

            allFuelRecords.AddRange(fuelRecords);
        }

        var appointments =
            (await _appointmentRepository
                .GetByUserIdAsync(userId))
            .ToList();

        var pendingAppointmentsCount =
            appointments.Count(x =>
                x.Status ==
                AppointmentStatus.Pending);

        var upcomingAppointments =
            appointments
                .Where(x =>
                    x.AppointmentDate >= DateTime.UtcNow &&
                    (
                        x.Status ==
                        AppointmentStatus.Pending ||
                        x.Status ==
                        AppointmentStatus.Accepted
                    ))
                .OrderBy(x => x.AppointmentDate)
                .Take(5)
                .ToList();

        var upcomingReminders =
            allReminders
                .Where(x =>
                    !x.IsCompleted)
                .OrderBy(x => x.ReminderDate)
                .Take(5)
                .ToList();

        var recentFuelRecords =
            allFuelRecords
                .OrderByDescending(x => x.FuelDate)
                .Take(5)
                .ToList();

        return new DashboardDto
        {
            TotalVehicles =
                vehicles.Count,

            ThisMonthExpenses =
                thisMonthExpenses,

            ActiveRemindersCount =
                allReminders.Count,

            PendingAppointmentsCount =
                pendingAppointmentsCount,

            UpcomingReminders =
                _mapper.Map<IEnumerable<ReminderDto>>(
                    upcomingReminders),

            RecentFuelRecords =
                _mapper.Map<IEnumerable<FuelRecordDto>>(
                    recentFuelRecords),

            UpcomingAppointments =
                _mapper.Map<IEnumerable<AppointmentDto>>(
                    upcomingAppointments)
        };
    }
}