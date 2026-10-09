using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Admin;

public class BusinessApplicationDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
    public BusinessApprovalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionComment { get; set; }
    public List<string> Photos { get; set; } = [];
}

public class RejectBusinessApplicationDto
{
    [Required, StringLength(1000)]
    public string Comment { get; set; } = null!;
}
