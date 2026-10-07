using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MotoGarage.Application.DTOs.Media;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Application.Mapping;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

internal static class TestSupport
{
    public static AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public static IMapper Mapper() => new MapperConfiguration(
        cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    public static UserManager<User> Users(AppDbContext context) => new(
        new UserStore<User, IdentityRole<int>, AppDbContext, int>(context),
        Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
        Array.Empty<IUserValidator<User>>(), Array.Empty<IPasswordValidator<User>>(),
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
        new ServiceCollection().BuildServiceProvider(), NullLogger<UserManager<User>>.Instance);

    public static AuthService Auth(AppDbContext context, UserManager<User> users, TestEmail? email = null) => new(
        users, new TestTokens(), new RefreshTokenRepository(context),
        new EmailVerificationCodeRepository(context), email ?? new TestEmail(),
        new ConfigurationBuilder().Build(), new TestUnitOfWork(context), NullLogger<AuthService>.Instance);

    public static OrderService Orders(AppDbContext context) => new(
        new OrderRepository(context), new ProductRepository(context), new StoreRepository(context),
        Mapper(), new TestUnitOfWork(context), new NotificationService(new NotificationRepository(context), Mapper()));

    public static async Task SeedOrderData(AppDbContext context, int stock = 1)
    {
        context.Users.AddRange(new User { Id = 1, FullName = "Customer", EmailConfirmed = true },
            new User { Id = 2, FullName = "Store", EmailConfirmed = true });
        context.Stores.Add(new Store
        {
            Id = 1,
            UserId = 2,
            StoreName = "Store",
            Slug = "store-test",
            IsActive = true,
            Description = "Store",
            PhoneNumber = "123",
            Address = "Baku"
        });
        context.Products.Add(new Product { Id = 1, StoreId = 1, Name = "Product", Slug = "product", Price = 10, Stock = stock, IsActive = true });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}

// In-memory tests exercise business behavior; SQL Server locking is checked separately through metadata/script tests.
internal sealed class TestUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        context.ChangeTracker.Clear();
        return await action();
    }
    public async Task ExecuteAsync(Func<Task> action) => await ExecuteAsync(async () => { await action(); return true; });
}

internal sealed class TestEmail : IEmailService
{
    public bool Fail { get; set; }
    public string? LastCode { get; private set; }
    public Task SendVerificationCodeAsync(string email, string fullName, string code)
    {
        LastCode = code;
        return Fail ? Task.FromException(new IOException("Test SMTP failure")) : Task.CompletedTask;
    }
}

internal sealed class TestTokens : ITokenService
{
    public string CreateAccessToken(User user) => "unit-test-access-token";
    public string CreateRefreshToken() => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    public DateTime GetAccessTokenExpiration() => DateTime.UtcNow.AddMinutes(30);
    public DateTime GetRefreshTokenExpiration() => DateTime.UtcNow.AddDays(7);
}

internal sealed class TestImages : IImageService
{
    public Task<UploadResultDto> UploadAsync(Stream stream, string fileName, int userId) => throw new NotSupportedException();
    public Task DeleteAsync(string publicId) => Task.CompletedTask;
    public Task ValidateOwnershipAsync(int userId, string? publicId, string? url) => Task.CompletedTask;
    public Task DeleteUnusedAsync(string? publicId, int userId) => Task.CompletedTask;
}
