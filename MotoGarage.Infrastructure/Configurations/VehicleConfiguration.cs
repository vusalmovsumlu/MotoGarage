using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.Property(x => x.ImagePublicId).HasMaxLength(255);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Engine)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.VIN)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PlateNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.CurrentKm)
            .IsRequired();

        builder.Property(x => x.VehicleType)
            .IsRequired();

        builder.Property(x => x.FuelType)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}