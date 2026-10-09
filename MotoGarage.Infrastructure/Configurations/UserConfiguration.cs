using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Role)
            .IsRequired();
        builder.Property(x => x.BusinessApprovalStatus).IsConcurrencyToken();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
