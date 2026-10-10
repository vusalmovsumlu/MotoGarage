using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Notifications;

public sealed class ReminderNotificationDispatcher(
    AppDbContext context,
    TimeProvider timeProvider,
    IOptions<ReminderNotificationOptions> options) : IReminderNotificationDispatcher
{
    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.ChangeTracker.Clear();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var due = await context.Reminders
                .Where(x => x.Vehicle.User.DeletedAtUtc == null && !x.IsCompleted && x.NotificationSentAtUtc == null &&
                    (x.ReminderDate <= now ||
                        (x.ReminderKm.HasValue && x.Vehicle.CurrentKm >= x.ReminderKm.Value)))
                .OrderBy(x => x.ReminderDate).ThenBy(x => x.Id)
                .Take(options.Value.BatchSize)
                .Select(x => new
                {
                    Reminder = x,
                    x.Vehicle.UserId,
                    x.Vehicle.Slug,
                    VehicleName = x.Vehicle.Brand + " " + x.Vehicle.Model,
                    x.Vehicle.PlateNumber
                })
                .ToListAsync(cancellationToken);

            if (due.Count == 0)
                return 0;

            foreach (var item in due)
            {
                var reminder = item.Reminder;
                reminder.NotificationSentAtUtc = now;
                context.Notifications.Add(new Notification
                {
                    UserId = item.UserId,
                    Type = NotificationType.Reminder,
                    Title = $"Xatırlatma: {reminder.Title}",
                    Message = $"{item.VehicleName} ({item.PlateNumber}): {reminder.Title} üçün planlaşdırdığınız tarix və ya odometr həddi çatıb.",
                    Link = $"/garage/{item.Slug}/reminders",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            try
            {
                // SQL Server commits notifications and their claims in one SaveChanges transaction.
                // Concurrency tokens also prevent sending a stale, rescheduled/completed reminder.
                await context.SaveChangesAsync(cancellationToken);
                return due.Count;
            }
            catch (ConflictException failure) when (
                failure.InnerException is DbUpdateConcurrencyException && attempt < 2)
            {
                // Another worker or an owner changed this batch: reload before retrying.
            }
        }
    }
}
