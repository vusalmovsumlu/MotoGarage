using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotoGarage.API.Controllers;
using MotoGarage.API.Hubs;
using MotoGarage.API.Middleware;
using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class AdminAuditFiltersTests
{
    private static readonly string[] Actions =
        ["UserBlocked", "UserUnblocked", "UserDeleted", "ApplicationApproved", "ApplicationRejected"];

    [Fact]
    public async Task HttpQueryFiltersActualMixedRecordsBeforeCountingAndPaging()
    {
        var database = Guid.NewGuid().ToString();
        await VerifyHttpFilters(options => options.UseInMemoryDatabase(database));
    }

    [LocalReminderSqlFact]
    public async Task HttpQueryFiltersMixedRecordsUsingRealSql()
    {
        using var configured = new AppDbContextFactory().CreateDbContext([]);
        var connection = new SqlConnectionStringBuilder(configured.Database.GetConnectionString());
        if (!connection.DataSource.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) ||
            connection.InitialCatalog != "MotoGarageDb")
            throw new InvalidOperationException("Only the configured LocalDB instance may be used for this test.");
        var databaseName = "MotoGarage_AdminAuditChecks_" + Guid.NewGuid().ToString("N");
        connection.InitialCatalog = databaseName;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var database = new AppDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            await VerifyHttpFilters(builder => builder.UseSqlServer(connection.ConnectionString));
        }
        finally
        {
            SqlConnection.ClearAllPools();
            if (connection.InitialCatalog != databaseName || !databaseName.StartsWith("MotoGarage_AdminAuditChecks_"))
                throw new InvalidOperationException("Unsafe test cleanup target.");
            await database.Database.EnsureDeletedAsync();
        }
    }

    private static async Task VerifyHttpFilters(Action<DbContextOptionsBuilder> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
        var services = builder.Services;
        services.AddAuthentication();
        services.AddAuthorization();
        services.AddControllers().AddApplicationPart(typeof(AdminController).Assembly);
        services.AddDbContext<AppDbContext>(configure);
        services.AddScoped<UserManager<User>>(sp => TestSupport.Users(sp.GetRequiredService<AppDbContext>()));
        services.AddSingleton<IUserConnectionRegistry, UserConnectionRegistry>();
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IBusinessApplicationService, BusinessApplicationService>();
        services.AddScoped<IAdminAuditService, AdminAuditService>();
        await using var app = builder.Build();
        app.UseMiddleware<ExceptionMiddleware>();
        // Only this isolated host accepts a test identity. No production workers run.
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")], "IsolatedTest"));
            await next();
        });
        app.UseAuthorization();
        app.MapControllers();
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var administrator = new User { FullName = "Admin", Role = UserRole.Admin, EmailConfirmed = true };
            context.Users.Add(administrator);
            await context.SaveChangesAsync();
            Assert.Equal(1, administrator.Id);
            var timestamp = new DateTime(2026, 10, 13, 8, 0, 0, DateTimeKind.Utc);
            context.AdminAuditLogs.AddRange(Enumerable.Range(1, 135).Select(id => new AdminAuditLog
            {
                AdminId = 1, AdminName = "Admin", Action = Actions[(id - 1) % Actions.Length],
                TargetType = "User", TargetId = id, TargetName = $"Target {id}",
                OccurredAtUtc = timestamp.AddMinutes(id / 3)
            }));
            await context.SaveChangesAsync();
        }
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            async Task<AdminAuditPage> Read(string query)
            {
                using var response = await client.GetAsync("/api/admin/audit-logs" + query);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return (await response.Content.ReadFromJsonAsync<AdminAuditPage>())!;
            }
            var all = await Read("");
            Assert.Equal(135, all.Total);
            Assert.Equal(25, all.Items.Count);
            Assert.Equal(Actions.Length, all.Items.Select(x => x.Action).Distinct().Count());
            foreach (var action in Actions)
            {
                var first = await Read($"?action={action}&page=1&pageSize=25");
                var second = await Read($"?action={action}&page=2&pageSize=25");
                Assert.Equal(27, first.Total);
                Assert.Equal(first.Total, second.Total);
                Assert.Equal(25, first.Items.Count);
                Assert.Equal(2, second.Items.Count);
                Assert.All(first.Items.Concat(second.Items), x => Assert.Equal(action, x.Action));
                Assert.Equal(27, first.Items.Concat(second.Items).Select(x => x.Id).Distinct().Count());
                var combined = first.Items.Concat(second.Items).ToList();
                Assert.Equal(combined.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id).Select(x => x.Id),
                    combined.Select(x => x.Id));
            }
            Assert.Equal(135, (await Read("?action=&page=1")).Total);
            using var invalid = await client.GetAsync("/api/admin/audit-logs?action=bad");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        finally { await app.StopAsync(); }
    }
}
