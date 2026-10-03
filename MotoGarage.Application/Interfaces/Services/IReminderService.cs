using MotoGarage.Application.DTOs.Reminder;

namespace MotoGarage.Application.Interfaces.Services;

public interface IReminderService
{
    Task<IEnumerable<ReminderDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId);

    Task<IEnumerable<ReminderDto>> GetActiveAsync(
        int vehicleId,
        int userId);

    Task<IEnumerable<ReminderDto>> GetDueAsync(
        int vehicleId,
        int userId);

    Task<ReminderDto> GetByIdAsync(
        int id,
        int userId);

    Task<ReminderDto> CreateAsync(
        int userId,
        CreateReminderDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateReminderDto dto);

    Task MarkAsCompletedAsync(
        int id,
        int userId);

    Task DeleteAsync(
        int id,
        int userId);
}