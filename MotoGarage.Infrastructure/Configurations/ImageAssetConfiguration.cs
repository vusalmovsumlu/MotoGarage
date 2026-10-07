using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class ImageAssetConfiguration : IEntityTypeConfiguration<ImageAsset>
{
    public void Configure(EntityTypeBuilder<ImageAsset> builder)
    {
        builder.HasKey(x => x.PublicId);
        builder.Property(x => x.PublicId).HasMaxLength(255);
        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
