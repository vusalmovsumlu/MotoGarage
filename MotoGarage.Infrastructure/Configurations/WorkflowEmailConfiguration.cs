using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class WorkflowEmailConfiguration : IEntityTypeConfiguration<WorkflowEmail>
{
    public void Configure(EntityTypeBuilder<WorkflowEmail> builder)
    {
        builder.Property(x => x.EventKey).HasMaxLength(100);
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.HasIndex(x => x.EventKey).IsUnique();
        builder.HasIndex(x => new { x.RecipientUserId, x.Id });
        builder.HasIndex(x => new { x.SentAtUtc, x.DiscardedAtUtc, x.FailedAtUtc, x.NextAttemptAtUtc });
        builder.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
