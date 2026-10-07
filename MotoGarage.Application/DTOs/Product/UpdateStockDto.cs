using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Product;

public class UpdateStockDto
{
    [Range(0, 10000000)]
    public int Stock { get; set; }
}