using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Notifications;

public sealed class AppointmentEmailReminderDispatcher(
    AppDbContext context,
    IEmailService email,
    TimeProvider clock,
    IOptions<AppointmentEmailReminderOptions> options,
    ILogger<AppointmentEmailReminderDispatcher> logger) : IAppointmentEmailReminderDispatcher
{
    IQueryable<Appointment> Due(DateTime now) => context.Appointments.Where(x =>
        x.EmailReminderSentAtUtc == null &&
        (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted) &&
        x.AppointmentDate > now && x.AppointmentDate <= now.AddHours(options.Value.LeadTimeHours) &&
        (x.EmailReminderLeaseUntilUtc == null || x.EmailReminderLeaseUntilUtc <= now) &&
        (x.EmailReminderNextAttemptAtUtc == null || x.EmailReminderNextAttemptAtUtc <= now) &&
        x.User.EmailConfirmed && !x.User.IsAdminBlocked && x.User.Email != null && x.User.Email != "");

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return 0;
        var ids = await Due(clock.GetUtcNow().UtcDateTime).AsNoTracking()
            .OrderBy(x => x.AppointmentDate).ThenBy(x => x.Id)
            .Select(x => x.Id).Take(options.Value.BatchSize).ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = clock.GetUtcNow().UtcDateTime;
            var leaseId = Guid.NewGuid();
            // Atomic conditional UPDATE is also a claim across multiple API instances.
            // Each item is claimed just before sending; no SMTP call runs inside an EF retry transaction.
            var claimed = await Due(now).Where(x => x.Id == id).ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EmailReminderLeaseId, (Guid?)leaseId)
                .SetProperty(x => x.EmailReminderLeaseUntilUtc, (DateTime?)now.AddMinutes(options.Value.LeaseMinutes)), cancellationToken);
            if (claimed == 0) continue;
            var smtpAccepted = false;
            try
            {
                // Recheck after the claim so a just-cancelled/completed appointment is discarded.
                var recheckAt = clock.GetUtcNow().UtcDateTime;
                var item = await context.Appointments.AsNoTracking().Where(x => x.Id == id && x.EmailReminderLeaseId == leaseId &&
                    x.EmailReminderSentAtUtc == null && x.AppointmentDate > recheckAt &&
                    x.AppointmentDate <= recheckAt.AddHours(options.Value.LeadTimeHours) && x.EmailReminderLeaseUntilUtc > recheckAt &&
                    (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted) &&
                    x.User.EmailConfirmed && !x.User.IsAdminBlocked && x.User.Email != null && x.User.Email != "")
                    .Select(x => new
                    {
                        Recipient = x.User.Email!,
                        Reminder = new AppointmentReminderEmail(x.Id, x.User.FullName, x.AppointmentDate,
                            x.Vehicle.Brand + " " + x.Vehicle.Model,
                            x.Mechanic.User.FullName != null && x.Mechanic.User.FullName != "" ? x.Mechanic.User.FullName : x.Mechanic.BusinessName,
                            x.Mechanic.Address, x.Status)
                    }).SingleOrDefaultAsync(cancellationToken);
                if (item == null)
                {
                    await Release(id, leaseId, null, cancellationToken);
                    continue;
                }
                var remaining = now.AddMinutes(options.Value.LeaseMinutes - 1) - clock.GetUtcNow().UtcDateTime;
                if (remaining <= TimeSpan.Zero)
                {
                    await Release(id, leaseId, null, cancellationToken);
                    continue;
                }
                using var timeout = new CancellationTokenSource(remaining, clock);
                using var sendToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                await email.SendAppointmentReminderAsync(item.Recipient, item.Reminder, sendToken.Token);
                smtpAccepted = true;
                var deliveredAt = clock.GetUtcNow().UtcDateTime;
                var persisted = await context.Appointments.Where(x => x.Id == id && x.EmailReminderLeaseId == leaseId)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(x => x.EmailReminderSentAtUtc, (DateTime?)deliveredAt)
                        .SetProperty(x => x.EmailReminderLeaseId, (Guid?)null)
                        .SetProperty(x => x.EmailReminderLeaseUntilUtc, (DateTime?)null)
                        .SetProperty(x => x.EmailReminderNextAttemptAtUtc, (DateTime?)null), cancellationToken);
                if (persisted == 0)
                    logger.LogWarning("Appointment {AppointmentId} email was accepted, but its lease changed before the delivery marker was saved.", id);
                sent++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // A stopped process leaves a recoverable lease.
            }
            catch (Exception failure)
            {
                logger.LogWarning(failure, "Appointment {AppointmentId} email reminder failed; SMTP accepted: {Accepted}.", id, smtpAccepted);
                if (!smtpAccepted)
                    await Release(id, leaseId, clock.GetUtcNow().UtcDateTime.AddMinutes(options.Value.RetryMinutes), cancellationToken);
                // If SMTP succeeded but persistence failed, retain the lease until expiry.
                // SMTP cannot guarantee exactly-once delivery across a process/database failure.
            }
        }
        return sent;
    }

    Task<int> Release(int id, Guid leaseId, DateTime? retryAt, CancellationToken cancellationToken) =>
        context.Appointments.Where(x => x.Id == id && x.EmailReminderLeaseId == leaseId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EmailReminderLeaseId, (Guid?)null)
                .SetProperty(x => x.EmailReminderLeaseUntilUtc, (DateTime?)null)
                .SetProperty(x => x.EmailReminderNextAttemptAtUtc, retryAt), cancellationToken);
}
