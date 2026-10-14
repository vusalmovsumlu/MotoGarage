using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotoGarage.Application.DTOs.Notification;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Email;

public sealed class WorkflowEmailDispatcher(AppDbContext context, IEmailService email, TimeProvider clock,
    IOptions<WorkflowEmailOptions> options, ILogger<WorkflowEmailDispatcher> logger) : IWorkflowEmailDispatcher
{
    private IQueryable<WorkflowEmail> Pending => context.WorkflowEmails.Where(x =>
        x.SentAtUtc == null && x.DiscardedAtUtc == null && x.FailedAtUtc == null);

    private IQueryable<WorkflowEmail> Due(DateTime now) => Pending.Where(x =>
        (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now) &&
        (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= now) &&
        !Pending.Any(previous => previous.RecipientUserId == x.RecipientUserId && previous.Id < x.Id));

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled) return 0;
        var sent = 0;
        for (var i = 0; i < options.Value.BatchSize; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = clock.GetUtcNow().UtcDateTime;
            var id = await Due(now).OrderBy(x => x.Id).Select(x => (long?)x.Id).FirstOrDefaultAsync(cancellationToken);
            if (id == null) break;
            var lease = Guid.NewGuid();
            var claimed = await Due(now).Where(x => x.Id == id).ExecuteUpdateAsync(set => set
                .SetProperty(x => x.LeaseId, (Guid?)lease)
                .SetProperty(x => x.LeaseUntilUtc, (DateTime?)now.AddMinutes(options.Value.LeaseMinutes))
                .SetProperty(x => x.Attempts, x => x.Attempts + 1), cancellationToken);
            if (claimed == 0) continue;
            var accepted = false;
            try
            {
                var item = await Pending.AsNoTracking().Where(x => x.Id == id && x.LeaseId == lease).Select(x => new
                {
                    x.Attempts, x.LeaseUntilUtc,
                    Content = new WorkflowEmailContent(x.DeliveryId, x.Subject, x.TextBody, x.HtmlBody),
                    x.Recipient.Email, x.Recipient.EmailConfirmed, x.Recipient.IsAdminBlocked, x.Recipient.DeletedAtUtc
                }).SingleOrDefaultAsync(cancellationToken);
                if (item == null) continue;
                if (!item.EmailConfirmed || item.IsAdminBlocked || item.DeletedAtUtc != null || string.IsNullOrWhiteSpace(item.Email))
                {
                    await Owned(id.Value, lease).ExecuteUpdateAsync(set => set
                        .SetProperty(x => x.DiscardedAtUtc, (DateTime?)clock.GetUtcNow().UtcDateTime)
                        .SetProperty(x => x.LeaseId, (Guid?)null).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null), cancellationToken);
                    continue;
                }
                if (item.Attempts > options.Value.MaxAttempts)
                {
                    await Failure(id.Value, lease, item.Attempts, cancellationToken);
                    continue;
                }
                var remaining = item.LeaseUntilUtc!.Value.AddMinutes(-1) - clock.GetUtcNow().UtcDateTime;
                if (remaining <= TimeSpan.Zero)
                {
                    await Failure(id.Value, lease, item.Attempts, cancellationToken);
                    continue;
                }
                var timeoutDuration = TimeSpan.FromSeconds(Math.Min(options.Value.SendTimeoutSeconds, remaining.TotalSeconds));
                using var timeout = new CancellationTokenSource(timeoutDuration, clock);
                using var sendToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                await email.SendWorkflowUpdateAsync(item.Email, item.Content, sendToken.Token);
                accepted = true;
                var deliveredAt = clock.GetUtcNow().UtcDateTime;
                var persisted = await Owned(id.Value, lease).ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.SentAtUtc, (DateTime?)deliveredAt)
                    .SetProperty(x => x.LeaseId, (Guid?)null).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.NextAttemptAtUtc, (DateTime?)null), cancellationToken);
                if (persisted == 0) logger.LogWarning("Workflow email {EmailId} was accepted but its delivery lease changed.", id);
                sent++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception failure)
            {
                logger.LogWarning(failure, "Workflow email {EmailId} failed; SMTP accepted: {Accepted}.", id, accepted);
                if (!accepted)
                {
                    var attempts = await Owned(id.Value, lease).Select(x => (int?)x.Attempts).SingleOrDefaultAsync(cancellationToken);
                    if (attempts.HasValue) await Failure(id.Value, lease, attempts.Value, cancellationToken);
                }
                // Keep the lease if SMTP accepted but the delivery marker could not be persisted.
                // Stable Message-ID helps retries; SMTP itself cannot guarantee exactly-once delivery.
            }
        }
        return sent;
    }

    private IQueryable<WorkflowEmail> Owned(long id, Guid lease) => context.WorkflowEmails.Where(x => x.Id == id && x.LeaseId == lease);

    private async Task Failure(long id, Guid lease, int attempts, CancellationToken token)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var exhausted = attempts >= options.Value.MaxAttempts;
        DateTime? retry = exhausted ? null : now.AddSeconds(Math.Min(3600, options.Value.RetryDelaySeconds * Math.Pow(2, attempts - 1)));
        await Owned(id, lease).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.LeaseId, (Guid?)null).SetProperty(x => x.LeaseUntilUtc, (DateTime?)null)
            .SetProperty(x => x.NextAttemptAtUtc, retry)
            .SetProperty(x => x.FailedAtUtc, exhausted ? (DateTime?)now : null), token);
        if (exhausted) logger.LogError("Workflow email {EmailId} exhausted its {Attempts} delivery attempts.", id, attempts);
    }
}
