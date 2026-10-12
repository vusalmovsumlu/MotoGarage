using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MotoGarage.Infrastructure.Notifications;

public sealed class NotificationTransactionInterceptor(NotificationChangeInterceptor changes) : DbTransactionInterceptor
{
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) => changes.Committed();
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    { changes.Committed(); return Task.CompletedTask; }
    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) => changes.Clear();
    public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    { changes.Clear(); return Task.CompletedTask; }
    public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData) => changes.Clear();
    public override Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
    { changes.Clear(); return Task.CompletedTask; }
    public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
    { changes.Begin(); return result; }
    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
    { changes.Begin(); return ValueTask.FromResult(result); }
}
