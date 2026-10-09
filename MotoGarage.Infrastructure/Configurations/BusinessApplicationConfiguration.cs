using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class BusinessApplicationConfiguration : IEntityTypeConfiguration<BusinessApplication>
{
    public void Configure(EntityTypeBuilder<BusinessApplication> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.Property(x => x.Status).IsConcurrencyToken();
        builder.Property(x => x.DecisionComment).HasMaxLength(1000);
        builder.HasOne(x => x.User).WithOne().HasForeignKey<BusinessApplication>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}

public class BusinessApplicationPhotoConfiguration : IEntityTypeConfiguration<BusinessApplicationPhoto>
{
    public void Configure(EntityTypeBuilder<BusinessApplicationPhoto> builder)
    {
        builder.Property(x => x.PublicId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.HasIndex(x => new { x.ApplicationId, x.Position }).IsUnique();
        builder.HasOne(x => x.Application).WithMany(x => x.Photos).HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}
