using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.ImagePublicId).HasMaxLength(255);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.LowStockThreshold).HasDefaultValue(5).HasSentinel(-1);
        builder.ToTable(t => t.HasCheckConstraint("CK_Products_LowStockThreshold", "[LowStockThreshold] BETWEEN 0 AND 10000000"));
        builder.ToTable("Products", table => table.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0"));
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Stock)
            .IsRequired();

        builder.Property(x => x.Brand)
            .HasMaxLength(100);

        builder.Property(x => x.VehicleCompatibility)
            .HasMaxLength(300);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Store)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Slug)
    .IsRequired()
    .HasMaxLength(200);

        builder.HasIndex(x => x.Slug)
            .IsUnique();
    }
}