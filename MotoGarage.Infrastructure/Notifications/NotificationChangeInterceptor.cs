using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Notifications;

// Scoped to the DbContext. Explicit transactions wait for Commit; normal
// SaveChanges publishes only after its implicit transaction has succeeded.
public sealed class NotificationChangeInterceptor(INotificationChangeQueue queue) : SaveChangesInterceptor
{
    readonly HashSet<int> pending = [];
    int[] saving = [];

    void Capture(DbContext? context)
    {
        context?.ChangeTracker.DetectChanges();
        saving = context?.ChangeTracker.Entries<Notification>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => x.Entity.UserId).Distinct().ToArray() ?? [];
    }

    void Saved(DbContext? context)
    {
        if (context?.Database.CurrentTransaction != null) pending.UnionWith(saving);
        else queue.Enqueue(saving);
        saving = [];
    }

    internal void Committed() { queue.Enqueue(pending); pending.Clear(); }
    internal void Begin() => pending.Clear();
    internal void Changed(int userId, DbContext context)
    {
        if (context.Database.CurrentTransaction != null) pending.Add(userId);
        else queue.Enqueue([userId]);
    }
    internal void Clear() { pending.Clear(); saving = []; }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { Capture(eventData.Context); return result; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Capture(eventData.Context); return ValueTask.FromResult(result); }
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    { Saved(eventData.Context); return result; }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    { Saved(eventData.Context); return ValueTask.FromResult(result); }
    public override void SaveChangesFailed(DbContextErrorEventData eventData) => saving = [];
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    { saving = []; return Task.CompletedTask; }
}
