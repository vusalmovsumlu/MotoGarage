using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Email;

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
                .Where(x => x.Vehicle.User.DeletedAtUtc == null && !x.IsCompleted &&
                    (x.NotificationSentAtUtc == null || (x.EmailQueuedAtUtc == null &&
                        x.Vehicle.User.EmailConfirmed && !x.Vehicle.User.IsAdminBlocked &&
                        x.Vehicle.User.Email != null && x.Vehicle.User.Email != "")) &&
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
                    x.Vehicle.PlateNumber,
                    x.Vehicle.CurrentKm,
                    x.Vehicle.User.FullName,
                    x.Vehicle.User.Email,
                    x.Vehicle.User.EmailConfirmed,
                    x.Vehicle.User.IsAdminBlocked
                })
                .ToListAsync(cancellationToken);

            if (due.Count == 0)
                return 0;

            var keys = due.Select(x => ReminderEmailBuilder.EventKey(x.Reminder)).ToList();
            var existingEmails = (await context.WorkflowEmails.AsNoTracking()
                .Where(x => keys.Contains(x.EventKey)).Select(x => x.EventKey)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
            foreach (var item in due)
            {
                var reminder = item.Reminder;
                if (reminder.NotificationSentAtUtc is null)
                {
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
                if (reminder.EmailQueuedAtUtc is null && item.EmailConfirmed && !item.IsAdminBlocked
                    && !string.IsNullOrWhiteSpace(item.Email))
                {
                    if (!existingEmails.Contains(ReminderEmailBuilder.EventKey(reminder)))
                        context.WorkflowEmails.Add(ReminderEmailBuilder.Build(reminder, item.UserId, item.FullName,
                            item.VehicleName, item.PlateNumber, item.CurrentKm, now));
                    reminder.EmailQueuedAtUtc = now;
                }
            }

            try
            {
                // SQL Server commits notifications, email snapshots and their claims atomically.
                // Concurrency tokens also prevent sending a stale, rescheduled/completed reminder.
                await context.SaveChangesAsync(cancellationToken);
                return due.Count;
            }
            catch (ConflictException failure) when (
                failure.InnerException is DbUpdateConcurrencyException && attempt < 2)
            {
                // Another worker or an owner changed this batch: reload before retrying.
            }
            catch (DuplicateValueException failure) when (attempt < 2 &&
                failure.IndexName == "IX_WorkflowEmails_EventKey")
            {
                // Another worker queued this occurrence: reload its committed claims.
            }
        }
    }
}
