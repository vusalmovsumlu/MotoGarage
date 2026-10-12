using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Scheduling;

public sealed class AppointmentPlanningService(AppDbContext context, IUnitOfWork work,
    IAccountAccessService accounts, IMapper mapper) : IAppointmentPlanningService
{
    // Working hours are wall-clock time in Baku; persisted appointments remain UTC.
    public static DateTime StartOfDay(DateOnly day) => DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue).AddHours(-4), DateTimeKind.Utc);
    static DateTime Utc(DateTime date) => date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();
    static List<WorkingHourDto> Defaults() => Enumerable.Range(0, 7).Select(day => new WorkingHourDto
    { DayOfWeek = day, IsOpen = day != 0, Start = TimeSpan.FromHours(9), End = TimeSpan.FromHours(18) }).ToList();

    async Task<Mechanic> OwnMechanic(int userId)
    {
        await accounts.RequireParticipantAsync(userId);
        return await context.Mechanics.SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive)
            ?? throw new ForbiddenException("Aktiv usta profili lazımdır.");
    }
    async Task<List<WorkingHourDto>> Hours(int mechanicId)
    {
        var hours = await context.MechanicWorkingHours.AsNoTracking().Where(x => x.MechanicId == mechanicId)
            .OrderBy(x => x.DayOfWeek).Select(x => new WorkingHourDto
            { DayOfWeek = x.DayOfWeek, IsOpen = x.IsOpen, Start = x.Start, End = x.End }).ToListAsync();
        return hours.Count == 0 ? Defaults() : hours;
    }
    public async Task<List<WorkingHourDto>> GetWorkingHoursAsync(int userId) => await Hours((await OwnMechanic(userId)).Id);

    public async Task SetWorkingHoursAsync(int userId, List<WorkingHourDto> hours)
    {
        if (hours is null || hours.Count != 7 || hours.Any(x => x is null) || hours.Select(x => x.DayOfWeek).Distinct().Count() != 7)
            throw new DomainException("Həftənin hər günü üçün bir iş intervalı göndərin.");
        foreach (var hour in hours)
        {
            DtoValidation.Validate(hour);
            if (hour.Start < TimeSpan.Zero || hour.End >= TimeSpan.FromDays(1) || hour.End < TimeSpan.Zero ||
                hour.Start >= TimeSpan.FromDays(1) || hour.Start.TotalMinutes % 15 != 0 || hour.End.TotalMinutes % 15 != 0 ||
                (hour.IsOpen && hour.Start >= hour.End))
                throw new DomainException("İş saatları 00:00–23:45 aralığında, 15 dəqiqə addımla olmalıdır.");
        }
        await work.ExecuteAsync(async () =>
        {
            var mechanic = await OwnMechanic(userId);
            // Existing bookings are retained when working hours change.
            var existing = await context.MechanicWorkingHours.Where(x => x.MechanicId == mechanic.Id).ToListAsync();
            foreach (var hour in hours)
            {
                var entity = existing.SingleOrDefault(x => x.DayOfWeek == hour.DayOfWeek);
                if (entity is null) { entity = new() { MechanicId = mechanic.Id, DayOfWeek = hour.DayOfWeek }; context.MechanicWorkingHours.Add(entity); }
                entity.IsOpen = hour.IsOpen; entity.Start = hour.Start; entity.End = hour.End;
            }
            await context.SaveChangesAsync();
        });
    }
    static void ValidateDuration(int duration)
    {
        if (duration < 15 || duration > 240 || duration % 15 != 0)
            throw new DomainException("Görüş müddəti 15–240 dəqiqə və 15-in qatı olmalıdır.");
    }
    async Task RequireMechanic(int id)
    {
        var ownerId = await context.Mechanics.Where(x => x.Id == id && x.IsActive).Select(x => (int?)x.UserId).SingleOrDefaultAsync()
            ?? throw new KeyNotFoundException("Usta tapılmadı.");
        await accounts.RequireParticipantAsync(ownerId);
    }
    public async Task<AvailabilityDto> GetAvailabilityAsync(int mechanicId, DateOnly date, int durationMinutes)
    {
        ValidateDuration(durationMinutes);
        await RequireMechanic(mechanicId);
        if (date.Year < 1900 || date == DateOnly.MaxValue) throw new DomainException("Görüş günü düzgün deyil.");
        var start = StartOfDay(date);
        if (start > DateTime.UtcNow.AddDays(90) || start.AddDays(1) < DateTime.UtcNow)
            throw new DomainException("Tarix qarşıdakı 90 gün ərzində olmalıdır.");
        var hour = (await Hours(mechanicId)).Single(x => x.DayOfWeek == (int)date.DayOfWeek);
        var slots = new List<DateTime>();
        if (!hour.IsOpen) return new("Asia/Baku", durationMinutes, slots);
        var end = start.AddDays(1);
        var busy = await context.Appointments.AsNoTracking().Where(x => x.MechanicId == mechanicId &&
            (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted) &&
            x.AppointmentDate < end && x.AppointmentDate.AddMinutes(x.DurationMinutes) > start)
            .Select(x => new { x.AppointmentDate, x.DurationMinutes }).ToListAsync();
        for (var slot = start.Add(hour.Start); slot.AddMinutes(durationMinutes) <= start.Add(hour.End); slot = slot.AddMinutes(15))
            if (slot > DateTime.UtcNow && slot <= DateTime.UtcNow.AddDays(90) && !busy.Any(x => slot < x.AppointmentDate.AddMinutes(x.DurationMinutes) && slot.AddMinutes(durationMinutes) > x.AppointmentDate))
                slots.Add(slot);
        return new("Asia/Baku", durationMinutes, slots);
    }
    public async Task ValidateSlotAsync(int mechanicId, int vehicleId, DateTime date, int durationMinutes, int? excludeId = null)
    {
        ValidateDuration(durationMinutes);
        date = Utc(date);
        await RequireMechanic(mechanicId);
        if (date <= DateTime.UtcNow || date > DateTime.UtcNow.AddDays(90)) throw new DomainException("Görüş qarşıdakı 90 gün ərzində olmalıdır.");
        var local = date.AddHours(4);
        var hour = (await Hours(mechanicId)).Single(x => x.DayOfWeek == (int)local.DayOfWeek);
        if (!hour.IsOpen || local.TimeOfDay < hour.Start || local.TimeOfDay.Add(TimeSpan.FromMinutes(durationMinutes)) > hour.End ||
            local.TimeOfDay.TotalMinutes % 15 != 0)
            throw new DomainException("Ustanın iş saatlarından boş vaxt seçin.");
        var end = date.AddMinutes(durationMinutes);
        // Called inside SERIALIZABLE transactions; indexed time-range reads also protect against concurrent inserts.
        if (await context.Appointments.AnyAsync(x => x.Id != excludeId && (x.MechanicId == mechanicId || x.VehicleId == vehicleId) &&
            (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted) &&
            x.AppointmentDate < end && x.AppointmentDate.AddMinutes(x.DurationMinutes) > date))
            throw new ConflictException("Ustanın və ya nəqliyyatın bu vaxtda başqa görüşü var.");
    }
    public async Task<List<AppointmentDto>> GetCalendarAsync(int userId, DateOnly from, DateOnly to)
    {
        if (from.Year < 1900 || to == DateOnly.MaxValue || to < from || to.DayNumber - from.DayNumber >= 31) throw new DomainException("Təqvim maksimum 31 günlük ola bilər.");
        var mechanic = await OwnMechanic(userId);
        var start = StartOfDay(from); var end = StartOfDay(to.AddDays(1));
        var entries = await context.Appointments.AsNoTracking().Include(x => x.User).Include(x => x.Vehicle)
            .Include(x => x.Mechanic).ThenInclude(x => x.User)
            .Where(x => x.MechanicId == mechanic.Id && x.AppointmentDate >= start && x.AppointmentDate < end)
            .OrderBy(x => x.AppointmentDate).ToListAsync();
        return mapper.Map<List<AppointmentDto>>(entries);
    }
    async Task<Appointment> Participant(int id, int userId)
    {
        await accounts.RequireParticipantAsync(userId);
        var appointment = await context.Appointments.Include(x => x.Mechanic).Include(x => x.Vehicle)
            .SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Görüş tapılmadı.");
        if (appointment.UserId != userId && appointment.Mechanic.UserId != userId) throw new ForbiddenException("Bu görüş sizə aid deyil.");
        return appointment;
    }
    static void RequireActive(Appointment appointment)
    {
        if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Accepted))
            throw new ConflictException("Yalnız aktiv görüşün vaxtı dəyişdirilə bilər.");
    }
    void Notify(int userId, int id, string title, string message) => context.Notifications.Add(new Notification
    { UserId = userId, Type = NotificationType.Appointment, Title = title, Message = message, Link = $"/appointments/{id}", CreatedAt = DateTime.UtcNow });
    public async Task ProposeAsync(int id, int userId, RescheduleAppointmentDto dto)
    {
        DtoValidation.Validate(dto);
        await work.ExecuteAsync(async () =>
        {
            var a = await Participant(id, userId); RequireActive(a);
            if (a.ProposedAppointmentDateUtc.HasValue) throw new ConflictException("Əvvəl mövcud vaxt təklifinə qərar verin.");
            var date = Utc(dto.AppointmentDate);
            if (date == a.AppointmentDate) throw new DomainException("Yeni vaxt hazırkı vaxtdan fərqli olmalıdır.");
            await ValidateSlotAsync(a.MechanicId, a.VehicleId, date, a.DurationMinutes, a.Id);
            a.ProposedAppointmentDateUtc = date; a.RescheduleProposedByUserId = userId; a.RescheduleRevision++;
            Notify(a.UserId == userId ? a.Mechanic.UserId : a.UserId, a.Id, "Görüşün vaxtına yeni təklif", "Yeni vaxtı görüşlər bölməsində təsdiqləyin və ya rədd edin.");
            await context.SaveChangesAsync();
        });
    }
    public async Task DecideRescheduleAsync(int id, int userId, AppointmentDecisionDto dto)
    {
        DtoValidation.Validate(dto);
        await work.ExecuteAsync(async () =>
        {
            var a = await Participant(id, userId); RequireActive(a);
            if (!a.ProposedAppointmentDateUtc.HasValue || a.RescheduleRevision != dto.Revision)
                throw new ConflictException("Vaxt təklifi dəyişib və ya artıq cavablandırılıb.");
            var withdrawing = a.RescheduleProposedByUserId == userId;
            if (dto.Approve && withdrawing) throw new ForbiddenException("Öz vaxt təklifinizi təsdiqləyə bilməzsiniz.");
            if (dto.Approve)
            {
                if (a.EmailReminderSentAtUtc == null && a.EmailReminderLeaseId.HasValue && a.EmailReminderLeaseUntilUtc > DateTime.UtcNow)
                    throw new ConflictException("Email xatırlatması göndərilir. Bitdikdən sonra yeni vaxtı yenidən təsdiqləyin.");
                await ValidateSlotAsync(a.MechanicId, a.VehicleId, a.ProposedAppointmentDateUtc.Value, a.DurationMinutes, a.Id);
                a.AppointmentDate = a.ProposedAppointmentDateUtc.Value;
                a.EmailReminderSentAtUtc = a.EmailReminderLeaseUntilUtc = a.EmailReminderNextAttemptAtUtc = null;
                a.EmailReminderLeaseId = null;
            }
            var recipient = withdrawing ? (a.UserId == userId ? a.Mechanic.UserId : a.UserId) : a.RescheduleProposedByUserId!.Value;
            Notify(recipient, a.Id, "Görüş vaxtı təklifi", dto.Approve ? "Yeni görüş vaxtı təsdiqləndi." : withdrawing ? "Vaxt təklifi geri götürüldü. Əvvəlki vaxt qüvvədədir." : "Yeni vaxt təklifi rədd edildi. Əvvəlki vaxt qüvvədədir.");
            a.ProposedAppointmentDateUtc = null; a.RescheduleProposedByUserId = null;
            await context.SaveChangesAsync();
        });
    }
    static ServiceReportDto ReportDto(AppointmentServiceReport r) => new(r.Id, r.AppointmentId, r.ServiceName, r.Description,
        r.ReplacedParts, r.Kilometer, r.Cost, r.ServiceDate, r.Status, r.Revision, r.DecisionComment, r.UpdatedAtUtc, r.DecidedAtUtc);
    public async Task<ServiceReportDto?> GetReportAsync(int id, int userId)
    {
        await Participant(id, userId);
        var report = await context.AppointmentServiceReports.AsNoTracking().SingleOrDefaultAsync(x => x.AppointmentId == id);
        return report is null ? null : ReportDto(report);
    }
    public async Task SubmitReportAsync(int id, int userId, SubmitServiceReportDto dto)
    {
        DtoValidation.Validate(dto);
        if (string.IsNullOrWhiteSpace(dto.ServiceName)) throw new DomainException("Görülən işin adını daxil edin.");
        if (decimal.Round(dto.Cost, 2) != dto.Cost) throw new DomainException("Məbləğ maksimum iki onluq rəqəm ola bilər.");
        dto.ServiceDate = Utc(dto.ServiceDate);
        if (dto.ServiceDate < new DateTime(1900, 1, 1) || dto.ServiceDate > DateTime.UtcNow.AddMinutes(1))
            throw new DomainException("Servis tarixi gələcək ola bilməz.");
        await work.ExecuteAsync(async () =>
        {
            var a = await Participant(id, userId);
            if (a.Mechanic.UserId != userId) throw new ForbiddenException("Hesabatı yalnız bu görüşün ustası yaza bilər.");
            if (a.Status != AppointmentStatus.Completed) throw new ConflictException("Əvvəl görüşü tamamlayın.");
            var report = await context.AppointmentServiceReports.SingleOrDefaultAsync(x => x.AppointmentId == id);
            if (report?.Status == "Approved") throw new ConflictException("Təsdiqlənmiş hesabat dəyişdirilə bilməz.");
            if ((report?.Revision ?? 0) != dto.Revision) throw new ConflictException("Hesabat dəyişib. Yenidən açın.");
            if (report is null) { report = new() { AppointmentId = id, Revision = 0 }; context.AppointmentServiceReports.Add(report); }
            report.ServiceName = dto.ServiceName.Trim(); report.Description = dto.Description; report.ReplacedParts = dto.ReplacedParts;
            report.Cost = dto.Cost; report.Kilometer = dto.Kilometer; report.ServiceDate = dto.ServiceDate;
            report.Status = "Pending"; report.Revision++; report.UpdatedAtUtc = DateTime.UtcNow;
            report.DecidedAtUtc = null; report.DecisionComment = null;
            Notify(a.UserId, id, "Servis hesabatı təsdiq gözləyir", "Ustanın hesabatını yoxlayın. Təsdiq etdikdə nəqliyyatın servis tarixçəsinə əlavə olunacaq.");
            await context.SaveChangesAsync();
        });
    }
    public async Task DecideReportAsync(int id, int userId, AppointmentDecisionDto dto)
    {
        DtoValidation.Validate(dto);
        if (!dto.Approve && string.IsNullOrWhiteSpace(dto.Comment)) throw new DomainException("Rədd səbəbini yazın.");
        await work.ExecuteAsync(async () =>
        {
            var a = await Participant(id, userId);
            if (a.UserId != userId || a.Vehicle.UserId != userId) throw new ForbiddenException("Hesabatı yalnız nəqliyyatın sahibi təsdiqləyə bilər.");
            var r = await context.AppointmentServiceReports.SingleOrDefaultAsync(x => x.AppointmentId == id)
                ?? throw new KeyNotFoundException("Hesabat tapılmadı.");
            if (r.Revision != dto.Revision || r.Status != "Pending") throw new ConflictException("Hesabat dəyişib və ya artıq cavablandırılıb.");
            if (a.Status != AppointmentStatus.Completed) throw new ConflictException("Görüş tamamlanmayıb.");
            r.Status = dto.Approve ? "Approved" : "Rejected"; r.DecidedAtUtc = DateTime.UtcNow; r.DecisionComment = dto.Approve ? null : dto.Comment!.Trim();
            if (dto.Approve)
            {
                context.ServiceRecords.Add(new ServiceRecord
                {
                    VehicleId = a.VehicleId,
                    ServiceName = r.ServiceName,
                    Description = r.Description,
                    ReplacedParts = r.ReplacedParts,
                    Cost = r.Cost,
                    Kilometer = r.Kilometer,
                    ServiceDate = r.ServiceDate
                });
                a.Vehicle.CurrentKm = Math.Max(a.Vehicle.CurrentKm, r.Kilometer);
            }
            Notify(a.Mechanic.UserId, id, "Servis hesabatına qərar", dto.Approve ? "Hesabat təsdiqləndi və servis tarixçəsinə əlavə edildi." : r.DecisionComment!);
            await context.SaveChangesAsync();
        });
    }
}
