using MotoGarage.Application.DTOs.Expense;

namespace MotoGarage.Application.Interfaces.Services;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId);

    Task<ExpenseDto> GetByIdAsync(
        int id,
        int userId);

    Task<ExpenseSummaryDto> GetByDateRangeAsync(
        int vehicleId,
        int userId,
        DateTime startDate,
        DateTime endDate);

    Task<ExpenseDto> CreateAsync(
        int userId,
        CreateExpenseDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateExpenseDto dto);

    Task DeleteAsync(
        int id,
        int userId);
}