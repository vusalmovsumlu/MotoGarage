using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.RecipientName).HasMaxLength(150);
        builder.Property(x => x.DeliveryPhone).HasMaxLength(30);
        builder.Property(x => x.DeliveryCity).HasMaxLength(100);
        builder.Property(x => x.DeliveryAddress).HasMaxLength(500);
        builder.Property(x => x.DeliveryNote).HasMaxLength(500);
        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Store)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
