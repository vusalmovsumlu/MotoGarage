using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Exceptions;
using AutoMapper;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public ReminderService(
        IReminderRepository reminderRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _reminderRepository = reminderRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ReminderDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        var reminders =
            await _reminderRepository
                .GetByVehicleIdAsync(vehicleId);

        return _mapper.Map<IEnumerable<ReminderDto>>(reminders);
    }

    public async Task<IEnumerable<ReminderDto>> GetActiveAsync(
        int vehicleId,
        int userId)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        var reminders =
            await _reminderRepository
                .GetActiveRemindersAsync(vehicleId);

        return _mapper.Map<IEnumerable<ReminderDto>>(reminders);
    }

    public async Task<IEnumerable<ReminderDto>> GetDueAsync(
        int vehicleId,
        int userId)
    {
        var vehicle =
            await ValidateVehicleOwnershipAsync(
                vehicleId,
                userId);

        var reminders =
            await _reminderRepository
                .GetActiveRemindersAsync(vehicleId);

        var now = DateTime.UtcNow;

        var dueReminders = reminders
            .Where(x =>
                x.ReminderDate <= now ||
                (
                    x.ReminderKm.HasValue &&
                    vehicle.CurrentKm >= x.ReminderKm.Value
                ))
            .ToList();

        return _mapper.Map<IEnumerable<ReminderDto>>(
            dueReminders);
    }

    public async Task<ReminderDto> GetByIdAsync(
        int id,
        int userId)
    {
        var reminder =
            await _reminderRepository.GetByIdAsync(id);

        if (reminder is null)
        {
            throw new KeyNotFoundException(
                "Xatırlatma tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            reminder.VehicleId,
            userId);

        return _mapper.Map<ReminderDto>(reminder);
    }

    public async Task<ReminderDto> CreateAsync(
        int userId,
        CreateReminderDto dto)
    {
        DtoValidation.Validate(dto);
        await ValidateVehicleOwnershipAsync(
            dto.VehicleId,
            userId);

        var reminder =
            _mapper.Map<Reminder>(dto);

        reminder.IsCompleted = false;

        await _reminderRepository.AddAsync(reminder);

        await _reminderRepository.SaveChangesAsync();

        return _mapper.Map<ReminderDto>(reminder);
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateReminderDto dto)
    {
        DtoValidation.Validate(dto);
        var reminder =
            await _reminderRepository.GetByIdAsync(id);

        if (reminder is null)
        {
            throw new KeyNotFoundException(
                "Xatırlatma tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            reminder.VehicleId,
            userId);

        var scheduleChanged = reminder.ReminderDate != dto.ReminderDate ||
            reminder.ReminderKm != dto.ReminderKm;
        _mapper.Map(dto, reminder);
        if (scheduleChanged)
        {
            reminder.NotificationSentAtUtc = null;
            reminder.EmailQueuedAtUtc = null;
            reminder.ScheduleRevision = checked(reminder.ScheduleRevision + 1);
        }

        await _reminderRepository.UpdateAsync(reminder);

        await _reminderRepository.SaveChangesAsync();
    }

    public async Task MarkAsCompletedAsync(
        int id,
        int userId)
    {
        var reminder =
            await _reminderRepository.GetByIdAsync(id);

        if (reminder is null)
        {
            throw new KeyNotFoundException(
                "Xatırlatma tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            reminder.VehicleId,
            userId);

        reminder.IsCompleted = true;

        await _reminderRepository.UpdateAsync(reminder);

        await _reminderRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        var reminder =
            await _reminderRepository.GetByIdAsync(id);

        if (reminder is null)
        {
            throw new KeyNotFoundException(
                "Xatırlatma tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            reminder.VehicleId,
            userId);

        await _reminderRepository.DeleteAsync(reminder);

        await _reminderRepository.SaveChangesAsync();
    }

    private async Task<Vehicle> ValidateVehicleOwnershipAsync(
        int vehicleId,
        int userId)
    {
        var vehicle =
            await _vehicleRepository.GetByIdAsync(vehicleId);

        if (vehicle is null)
        {
            throw new KeyNotFoundException(
                "Nəqliyyat vasitəsi tapılmadı.");
        }

        if (vehicle.UserId != userId)
        {
            throw new ForbiddenException(
                "Bu nəqliyyat vasitəsinə giriş icazən yoxdur.");
        }

        return vehicle;
    }
}
