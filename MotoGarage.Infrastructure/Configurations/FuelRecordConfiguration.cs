using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class FuelRecordConfiguration : IEntityTypeConfiguration<FuelRecord>
{
    public void Configure(EntityTypeBuilder<FuelRecord> builder)
    {
        builder.Property(x => x.Liters)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PricePerLiter)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Kilometer)
            .IsRequired();

        builder.Property(x => x.FuelDate)
            .IsRequired();

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.FuelRecords)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}