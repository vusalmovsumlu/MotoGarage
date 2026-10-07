namespace MotoGarage.Domain.Entities;

public class EmailVerificationCode
{
    public int Id { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int UserId { get; set; }

    public string CodeHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int FailedAttempts { get; set; }

    public User User { get; set; } = null!;
}