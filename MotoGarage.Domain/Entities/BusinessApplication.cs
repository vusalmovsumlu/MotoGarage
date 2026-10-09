using MotoGarage.Domain.Enums;

namespace MotoGarage.Domain.Entities;

public class BusinessApplication
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public BusinessApprovalStatus Status { get; set; } = BusinessApprovalStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedById { get; set; }
    public string? DecisionComment { get; set; }
    public ICollection<BusinessApplicationPhoto> Photos { get; set; } = new List<BusinessApplicationPhoto>();
}

public class BusinessApplicationPhoto
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public BusinessApplication Application { get; set; } = null!;
    public int Position { get; set; }
    public string PublicId { get; set; } = null!;
    public string Url { get; set; } = null!;
}
