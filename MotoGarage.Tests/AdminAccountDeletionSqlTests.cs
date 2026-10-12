using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.API.Hubs;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Application.Services;

namespace MotoGarage.Tests;

public class AdminAccountDeletionSqlTests
{
    [LocalReminderSqlFact]
    public async Task MigrationPreservesAccountsAndDeletionRacingNewOrderCannotLeaveAnActiveOrderForADeletedCustomer()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_AccountDeletionChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString, sql => sql.EnableRetryOnFailure()).Options;
        await using var database = new AppDbContext(options);
        try
        {
            var previous = database.Database.GetMigrations().TakeWhile(x => !x.EndsWith("AddAdminAccountDeletion")).Last();
            await database.GetService<IMigrator>().MigrateAsync(previous);
            await database.Database.ExecuteSqlRawAsync("""
                INSERT INTO [AspNetUsers] ([FullName], [Role], [BusinessApprovalStatus], [CreatedAt], [EmailConfirmed], [IsAdminBlocked], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount])
                VALUES (N'Existing Test Account', 1, 2, SYSUTCDATETIME(), 1, 0, 0, 0, 0, 0);
                """);
            await database.Database.MigrateAsync();
            Assert.Null((await database.Users.SingleAsync()).DeletedAtUtc);
            // SQL Server generates entity IDs; seed the graph without explicit IDENTITY values.
            await database.Database.ExecuteSqlRawAsync("DELETE FROM [AspNetUsers]");
            using var users = TestSupport.Users(database);
            var admin = new MotoGarage.Domain.Entities.User { FullName = "Admin", UserName = "admin@example.invalid", Email = "admin@example.invalid", EmailConfirmed = true, Role = UserRole.Admin };
            var customer = new MotoGarage.Domain.Entities.User { FullName = "Customer", UserName = "customer@example.invalid", Email = "customer@example.invalid", EmailConfirmed = true, Role = UserRole.User };
            var seller = new MotoGarage.Domain.Entities.User { FullName = "Seller", UserName = "seller@example.invalid", Email = "seller@example.invalid", EmailConfirmed = true, Role = UserRole.Store };
            foreach (var user in new[] { admin, customer, seller }) Assert.True((await users.CreateAsync(user, "pass123")).Succeeded);
            var store = new MotoGarage.Domain.Entities.Store { UserId = seller.Id, StoreName = "Test", Slug = "test-store", Address = "Test", PhoneNumber = "Test", Description = "Test", IsActive = true };
            database.Stores.Add(store);
            var product = new MotoGarage.Domain.Entities.Product { Store = store, Name = "Test", Slug = "test-product", Stock = 3, Price = 45, IsActive = true };
            database.Products.Add(product);
            await database.SaveChangesAsync();

            async Task<Exception?> Delete()
            {
                await using var context = new AppDbContext(options);
                using var manager = TestSupport.Users(context);
                try { await new AdminService(context, manager, new UnitOfWork(context), new UserConnectionRegistry()).DeleteUserAsync(customer.Id, admin.Id); return null; }
                catch (Exception failure) { return failure; }
            }
            async Task<Exception?> Order()
            {
                await using var context = new AppDbContext(options);
                var service = new OrderService(new OrderRepository(context), new ProductRepository(context), new StoreRepository(context), TestSupport.Mapper(), new UnitOfWork(context), new NotificationService(new NotificationRepository(context), TestSupport.Mapper()), new AccountAccessService(context), new MotoGarage.Infrastructure.Inventory.StockAlertService(context));
                try { await service.CreateAsync(customer.Id, new() { Items = [new() { ProductId = product.Id, Quantity = 1 }] }); return null; }
                catch (Exception failure) { return failure; }
            }
            var results = await Task.WhenAll(Delete(), Order());
            database.ChangeTracker.Clear();
            var deleted = (await database.Users.SingleAsync(x => x.Id == customer.Id)).DeletedAtUtc != null;
            var count = await database.Orders.CountAsync(x => x.UserId == customer.Id);
            if (deleted)
            {
                Assert.Null(results[0]);
                Assert.IsType<UnauthorizedAccessException>(results[1]);
                Assert.Equal(0, count);
                Assert.Equal(3, (await database.Products.SingleAsync()).Stock);
            }
            else
            {
                Assert.IsType<ConflictException>(results[0]);
                Assert.Null(results[1]);
                Assert.Equal(1, count);
                Assert.Equal(2, (await database.Products.SingleAsync()).Stock);
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (!databaseName.StartsWith("MotoGarage_AccountDeletionChecks_", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected cleanup target.");
            await database.Database.EnsureDeletedAsync();
        }
    }
}
