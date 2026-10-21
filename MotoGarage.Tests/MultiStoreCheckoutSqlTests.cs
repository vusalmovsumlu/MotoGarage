using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class MultiStoreCheckoutSqlTests
{
    [LocalReminderSqlFact]
    public async Task CheckoutIsAtomicAcrossStoresIncludingMessagesAndConcurrentStockReservations()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Use the isolated LocalDB test configuration.");
        var name = "MotoGarage_MultiStoreChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = name;
        DbContextOptions<AppDbContext> Options(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString,
                sql => sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(300), [1205]));
            if (interceptor is not null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }

        await using var context = new AppDbContext(Options());
        try
        {
            await context.Database.MigrateAsync();
            var data = await MultiStoreCheckoutTests.SeedAsync(context);
            var result = await TestSupport.Orders(context, new UnitOfWork(context))
                .CheckoutAsync(data.Customer.Id, MultiStoreCheckoutTests.Cart(data));
            Assert.Equal(3, result.Orders.Count);
            Assert.Equal(61.75m, result.TotalAmount);
            Assert.Equal(3, await context.WorkflowEmails.CountAsync());
            Assert.Equal(3, await context.Notifications.CountAsync(x => x.Type == NotificationType.Order));
            var initialStock = await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync();
            Assert.Equal(new[] { 8, 9, 9, 7 }, initialStock);

            // The last store fails after earlier orders and email snapshots have
            // already been saved inside the still-uncommitted transaction.
            await using (var failed = new AppDbContext(Options(new RejectStoreNotification(data.Stores[^1].UserId))))
                await Assert.ThrowsAsync<DomainException>(() => TestSupport.Orders(failed, new UnitOfWork(failed))
                    .CheckoutAsync(data.Customer.Id, MultiStoreCheckoutTests.Cart(data)));
            context.ChangeTracker.Clear();
            Assert.Equal(3, await context.Orders.CountAsync());
            Assert.Equal(3, await context.WorkflowEmails.CountAsync());
            Assert.Equal(3, await context.Notifications.CountAsync(x => x.Type == NotificationType.Order));
            Assert.Equal(initialStock, await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync());

            var products = await context.Products.OrderBy(x => x.Id).ToListAsync();
            foreach (var product in products) product.Stock = 10;
            products[^1].Stock = 3;
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var barrier = new ProductReadBarrier();
            async Task<bool> Checkout()
            {
                await using var attempt = new AppDbContext(Options(barrier));
                try
                {
                    await TestSupport.Orders(attempt, new UnitOfWork(attempt))
                        .CheckoutAsync(data.Customer.Id, MultiStoreCheckoutTests.Cart(data));
                    return true;
                }
                catch (DomainException) { return false; }
            }
            var attempts = await Task.WhenAll(Checkout(), Checkout());
            Assert.Single(attempts.Where(x => x));
            context.ChangeTracker.Clear();
            Assert.Equal(new[] { 8, 9, 9, 0 }, await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync());
            Assert.Equal(6, await context.Orders.CountAsync());
            Assert.Equal(6, await context.WorkflowEmails.CountAsync());
            Assert.Equal(6, await context.Notifications.CountAsync(x => x.Type == NotificationType.Order));
            Assert.All(await context.Orders.Include(x => x.OrderItems).ThenInclude(x => x.Product).ToListAsync(), order =>
                Assert.All(order.OrderItems, item => Assert.Equal(order.StoreId, item.Product.StoreId)));
        }
        finally
        {
            var target = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
            if (target.DataSource != connection.DataSource || target.InitialCatalog != name
                || !name.StartsWith("MotoGarage_MultiStoreChecks_"))
                throw new InvalidOperationException("Unsafe integration database cleanup target.");
            await context.Database.EnsureDeletedAsync();
        }
    }

    private sealed class RejectStoreNotification(int userId) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (data.Context!.ChangeTracker.Entries<Notification>().Any(x => x.State == EntityState.Added
                && x.Entity.Type == NotificationType.Order && x.Entity.UserId == userId))
                throw new DomainException("Test notification failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ProductReadBarrier : DbCommandInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
            CommandExecutedEventData data, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM [Products]") && Interlocked.Increment(ref arrivals) <= 2)
            {
                if (Volatile.Read(ref arrivals) == 2) ready.TrySetResult();
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }
}
