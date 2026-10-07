using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Expense;

public class CreateExpenseDto
{
    public int VehicleId { get; set; }

    [EnumDataType(typeof(ExpenseType))]
    public ExpenseType ExpenseType { get; set; }

    [Required, StringLength(300)]
    public string Description { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public DateTime ExpenseDate { get; set; }
}