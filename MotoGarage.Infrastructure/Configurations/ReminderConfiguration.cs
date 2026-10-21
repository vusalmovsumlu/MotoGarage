using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ReminderDate)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(x => x.ReminderKm)
            .IsConcurrencyToken();

        builder.Property(x => x.NotificationSentAtUtc)
            .IsConcurrencyToken();

        builder.Property(x => x.EmailQueuedAtUtc)
            .IsConcurrencyToken();

        builder.Property(x => x.ScheduleRevision)
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(x => x.IsCompleted)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.ReminderDate, x.VehicleId })
            .HasDatabaseName("IX_Reminders_PendingNotification")
            .HasFilter("[IsCompleted] = 0 AND [NotificationSentAtUtc] IS NULL");

        builder.HasIndex(x => new { x.ReminderDate, x.VehicleId, x.ScheduleRevision })
            .HasDatabaseName("IX_Reminders_PendingEmail")
            .HasFilter("[IsCompleted] = 0 AND [EmailQueuedAtUtc] IS NULL");

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.Reminders)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
