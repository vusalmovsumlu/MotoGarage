namespace MotoGarage.Application.DTOs.Expense;

public class ExpenseSummaryDto
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal TotalAmount { get; set; }

    public IEnumerable<ExpenseDto> Expenses { get; set; }
        = new List<ExpenseDto>();
}