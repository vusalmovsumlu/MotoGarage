using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Services;
using System.Data;

namespace MotoGarage.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (_context.Database.CurrentTransaction is not null)
            return await action();

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // A deadlock retry must re-read data, never reuse stale tracked state.
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await action();
            await transaction.CommitAsync();
            return result;
        });
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        });
    }
}
