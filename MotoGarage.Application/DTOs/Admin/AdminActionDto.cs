using System.ComponentModel.DataAnnotations;
namespace MotoGarage.Application.DTOs.Admin;

public class AdminActionDto
{
    [StringLength(1000)] public string? Reason { get; set; }
}
