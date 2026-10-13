using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AttachmentId).IsUnique().HasFilter("[AttachmentId] IS NOT NULL");
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.SentAt)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SenderUser)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
