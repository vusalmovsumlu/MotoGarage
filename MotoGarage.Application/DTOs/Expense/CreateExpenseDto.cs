using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Expense;

public class CreateExpenseDto
{
    public int VehicleId { get; set; }

    public ExpenseType ExpenseType { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }
}