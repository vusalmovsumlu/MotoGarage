namespace MotoGarage.Domain.Entities;

public class ImageAsset
{
    public string PublicId { get; set; } = null!;
    public int UserId { get; set; }
    public string Url { get; set; } = null!;
    public bool IsDeleting { get; set; }
    public DateTime CreatedAt { get; set; }
}
