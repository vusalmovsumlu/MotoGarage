using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class BusinessApplicationSqlTests
{
    // Explicit opt-in; only a randomly named local test database is created and removed.
    [LocalReminderSqlFact]
    public async Task MigrationPreservesExistingAccountsAndConcurrentReviewsCommitOnlyOneDecision()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        var server = connection.DataSource;
        var local = server.Equals("localhost", StringComparison.OrdinalIgnoreCase) || server.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals(".") || server.StartsWith(".\\") || server.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase)
            || server.Equals("127.0.0.1") || server.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            || server.StartsWith(Environment.MachineName + "\\", StringComparison.OrdinalIgnoreCase);
        if (!local || connection.InitialCatalog != "MotoGarageDb") throw new InvalidOperationException("Unexpected configured database.");
        var databaseName = "MotoGarage_ApprovalChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        DbContextOptions<AppDbContext> Options(IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString);
            if (interceptor != null) builder.AddInterceptors(interceptor);
            return builder.Options;
        }
        await using var database = new AppDbContext(Options());
        try
        {
            var previousMigration = database.Database.GetMigrations().TakeWhile(x => !x.EndsWith("AddBusinessRegistrationApproval")).Last();
            await database.GetService<IMigrator>().MigrateAsync(previousMigration);
            await database.Database.ExecuteSqlRawAsync("""
                INSERT INTO [AspNetUsers] ([FullName], [Role], [CreatedAt], [EmailConfirmed], [IsAdminBlocked], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount])
                VALUES (N'Existing Store', 3, SYSUTCDATETIME(), 1, 0, 0, 0, 0, 0);
                """);
            await database.Database.MigrateAsync();
            var existing = await database.Users.SingleAsync();
            Assert.Equal(BusinessApprovalStatus.Approved, existing.BusinessApprovalStatus);
            Assert.Empty(database.BusinessApplications);

            var admin = new User { FullName = "Admin", Role = UserRole.Admin, EmailConfirmed = true };
            database.Users.Add(admin);
            await database.SaveChangesAsync();
            async Task<BusinessApplication> Seed()
            {
                var application = new BusinessApplication
                {
                    User = new User { FullName = "New Applicant", Role = UserRole.Mechanic, EmailConfirmed = true, BusinessApprovalStatus = BusinessApprovalStatus.Pending },
                    CreatedAt = DateTime.UtcNow,
                    Photos = Enumerable.Range(1, 3).Select(i => new BusinessApplicationPhoto
                    { Position = i, PublicId = "test/" + Guid.NewGuid(), Url = $"https://res.cloudinary.com/test/{i}.png" }).ToList()
                };
                database.BusinessApplications.Add(application);
                await database.SaveChangesAsync();
                database.ChangeTracker.Clear();
                return application;
            }
            var application = await Seed();
            var queries = new BusinessApplicationService(database);
            Assert.Equal(3, (await queries.GetMineAsync(application.UserId))!.Photos.Count);
            Assert.Single(await queries.GetApplicationsAsync(BusinessApprovalStatus.Pending));
            var barrier = new ReviewBarrier();
            await using (var first = new AppDbContext(Options(barrier)))
            await using (var second = new AppDbContext(Options(barrier)))
            {
                async Task<bool> Review(AppDbContext context, bool approve)
                {
                    try { await new BusinessApplicationService(context).ReviewAsync(application.Id, admin.Id, approve, "Real qaraj şəkilləri tələb olunur."); return true; }
                    catch (ConflictException) { return false; }
                }
                var results = await Task.WhenAll(Review(first, true), Review(second, false));
                Assert.Single(results.Where(x => x));
            }
            var decided = await database.BusinessApplications.Include(x => x.User).SingleAsync(x => x.Id == application.Id);
            Assert.NotEqual(BusinessApprovalStatus.Pending, decided.Status);
            Assert.Equal(decided.Status, decided.User.BusinessApprovalStatus);
            Assert.Single(await database.Notifications.Where(x => x.UserId == application.UserId).ToListAsync());
            Assert.Single(await database.AdminAuditLogs.Where(x => x.TargetType == "BusinessApplication" && x.TargetId == application.Id).ToListAsync());

            await database.Notifications.ExecuteDeleteAsync();
            var failing = await Seed();
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] ADD CONSTRAINT [CK_Test_ApprovalNotification] CHECK ([Title] <> N'Biznes müraciətiniz təsdiqləndi');");
            await using (var attempt = new AppDbContext(Options()))
                await Assert.ThrowsAsync<ConflictException>(() => new BusinessApplicationService(attempt).ReviewAsync(failing.Id, admin.Id, true, null));
            database.ChangeTracker.Clear();
            var unchanged = await database.BusinessApplications.Include(x => x.User).SingleAsync(x => x.Id == failing.Id);
            Assert.Equal(BusinessApprovalStatus.Pending, unchanged.Status);
            Assert.Equal(BusinessApprovalStatus.Pending, unchanged.User.BusinessApprovalStatus);
            Assert.Null(unchanged.ReviewedAt);
            Assert.Empty(await database.Notifications.Where(x => x.UserId == failing.UserId).ToListAsync());
            Assert.Empty(await database.AdminAuditLogs.Where(x => x.TargetType == "BusinessApplication" && x.TargetId == failing.Id).ToListAsync());
            await database.Database.ExecuteSqlRawAsync("ALTER TABLE [Notifications] DROP CONSTRAINT [CK_Test_ApprovalNotification];");
            Console.WriteLine("PASS: migration preserves existing accounts; concurrent reviews commit one decision; notification failures roll back approval.");
        }
        finally
        {
            var target = new SqlConnectionStringBuilder(database.Database.GetConnectionString()).InitialCatalog;
            if (target != databaseName || !target.StartsWith("MotoGarage_ApprovalChecks_") || target == "MotoGarageDb")
                throw new InvalidOperationException("Unsafe test database cleanup target.");
            await database.Database.EnsureDeletedAsync();
        }
    }

    sealed class ReviewBarrier : SaveChangesInterceptor
    {
        int arrivals;
        readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            return result;
        }
    }
}
