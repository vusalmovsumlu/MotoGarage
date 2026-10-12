using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class WorkingHourConfiguration : IEntityTypeConfiguration<MechanicWorkingHour>
{
    public void Configure(EntityTypeBuilder<MechanicWorkingHour> b)
    {
        b.HasIndex(x => new { x.MechanicId, x.DayOfWeek }).IsUnique();
        b.HasOne(x => x.Mechanic).WithMany().HasForeignKey(x => x.MechanicId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_WorkingHours_Range", "[DayOfWeek] BETWEEN 0 AND 6 AND ([IsOpen] = 0 OR [Start] < [End])"));
    }
}

public class ServiceReportConfiguration : IEntityTypeConfiguration<AppointmentServiceReport>
{
    public void Configure(EntityTypeBuilder<AppointmentServiceReport> b)
    {
        b.HasIndex(x => x.AppointmentId).IsUnique();
        b.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.Property(x => x.ServiceName).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ReplacedParts).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(20);
        b.Property(x => x.DecisionComment).HasMaxLength(1000);
        b.Property(x => x.Cost).HasPrecision(18, 2);
        b.ToTable(t => t.HasCheckConstraint("CK_ServiceReports_Values", "[Cost] >= 0 AND [Kilometer] >= 0 AND [Revision] > 0 AND [Status] IN ('Pending', 'Approved', 'Rejected')"));
    }
}

public class AdminAuditConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> b)
    {
        b.HasIndex(x => new { x.OccurredAtUtc, x.Id });
        b.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.AdminName).HasMaxLength(200);
        b.Property(x => x.TargetName).HasMaxLength(200);
        b.Property(x => x.TargetType).HasMaxLength(50);
        b.Property(x => x.Action).HasMaxLength(50);
        b.Property(x => x.Reason).HasMaxLength(1000);
    }
}
