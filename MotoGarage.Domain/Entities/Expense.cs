using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class Expense
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public ExpenseType ExpenseType { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}