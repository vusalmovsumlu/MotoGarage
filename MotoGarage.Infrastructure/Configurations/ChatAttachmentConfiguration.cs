using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class ChatAttachmentConfiguration : IEntityTypeConfiguration<ChatAttachment>
{
    public void Configure(EntityTypeBuilder<ChatAttachment> b)
    {
        b.Property(x => x.PublicId).IsRequired().HasMaxLength(255);
        b.Property(x => x.Format).IsRequired().HasMaxLength(10);
        b.Property(x => x.Kind).IsRequired().HasMaxLength(10);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(50);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.PublicId).IsUnique();
        b.HasIndex(x => new { x.IsAttached, x.CreatedAtUtc });
        b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_ChatAttachments_KindSize", "([Kind] = 'image' AND [SizeBytes] > 0 AND [SizeBytes] <= 5242880) OR ([Kind] = 'video' AND [SizeBytes] > 0 AND [SizeBytes] <= 26214400)"));
    }
}
