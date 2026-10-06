using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Reminder;

namespace MotoGarage.Application.DTOs.Dashboard;

public class DashboardDto
{
    public int TotalVehicles { get; set; }

    public decimal ThisMonthExpenses { get; set; }

    public int ActiveRemindersCount { get; set; }

    public int PendingAppointmentsCount { get; set; }

    public IEnumerable<ReminderDto> UpcomingReminders { get; set; }
        = new List<ReminderDto>();

    public IEnumerable<FuelRecordDto> RecentFuelRecords { get; set; }
        = new List<FuelRecordDto>();

    public IEnumerable<AppointmentDto> UpcomingAppointments { get; set; }
        = new List<AppointmentDto>();
}