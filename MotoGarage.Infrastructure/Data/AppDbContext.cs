using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MotoGarage.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ImageAsset> ImageAssets { get; set; }

    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ServiceRecord> ServiceRecords { get; set; }
    public DbSet<FuelRecord> FuelRecords { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<Mechanic> Mechanics { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("Məlumat başqa əməliyyatla dəyişdirilib. Yenidən yoxlayın.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql &&
            (sql.Number == 2601 || sql.Number == 2627))
        {
            var match = Regex.Match(ex.InnerException!.Message, @"(?:IX|UX)_[A-Za-z0-9_]+");
            throw new DuplicateValueException(match.Value, ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number == 547)
        {
            throw new ConflictException("Əlaqəli məlumat və ya database qaydası bu əməliyyata icazə vermir.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly
        );
    }
}