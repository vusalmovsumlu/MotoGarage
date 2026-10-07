using MotoGarage.Application.DTOs.Auth;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Infrastructure.Administration;
using MotoGarage.API.Hubs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MotoGarage.Tests;

public class BusinessTests
{
    [Fact]
    public async Task StoreCancellationRestoresStockOnce()
    {
        using var context = TestSupport.Context();
        await TestSupport.SeedOrderData(context);
        var service = TestSupport.Orders(context);
        var order = await service.CreateAsync(1, OrderRequest());
        Assert.Equal(0, (await context.Products.SingleAsync()).Stock);
        await service.UpdateStatusAsync(order.Id, 2, new() { Status = OrderStatus.Cancelled });
        Assert.Equal(1, (await context.Products.SingleAsync()).Stock);
        await service.CancelAsync(order.Id, 1);
        Assert.Equal(1, (await context.Products.SingleAsync()).Stock);
        Assert.Equal(2, await context.Notifications.CountAsync());
    }

    [Fact]
    public async Task CustomerCannotCancelAnotherUsersOrder()
    {
        using var context = TestSupport.Context();
        await TestSupport.SeedOrderData(context);
        var service = TestSupport.Orders(context);
        var order = await service.CreateAsync(1, OrderRequest());
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelAsync(order.Id, 2));
        Assert.Equal(0, (await context.Products.SingleAsync()).Stock);
    }

    [Fact]
    public async Task OutOfStockOrderIsRejected()
    {
        using var context = TestSupport.Context();
        await TestSupport.SeedOrderData(context, 0);
        await Assert.ThrowsAsync<DomainException>(() => TestSupport.Orders(context).CreateAsync(1, OrderRequest()));
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task RefreshRotationStoresOnlyHashAndReuseRevokesReplacement()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var user = new User { UserName = "user@test.example", Email = "user@test.example", FullName = "User", EmailConfirmed = true };
        Assert.True((await users.CreateAsync(user, "pass123")).Succeeded);
        var auth = TestSupport.Auth(context, users);
        var login = await auth.LoginAsync(new() { Email = user.Email, Password = "pass123" });
        var token = await context.RefreshTokens.SingleAsync();
        Assert.Equal(MotoGarage.Infrastructure.Authentication.TokenHash.Compute(login.RefreshToken), token.Token);
        Assert.NotEqual(login.RefreshToken, token.Token);
        var rotated = await auth.RefreshTokenAsync(new() { RefreshToken = login.RefreshToken });
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.RefreshTokenAsync(new() { RefreshToken = login.RefreshToken }));
        Assert.All(context.RefreshTokens, value => Assert.NotNull(value.RevokedAt));
    }

    [Fact]
    public async Task LogoutCannotRevokeAnotherUsersSession()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        context.Users.Add(new User { Id = 1, FullName = "User", EmailConfirmed = true });
        context.RefreshTokens.Add(new RefreshToken { UserId = 1, Token = MotoGarage.Infrastructure.Authentication.TokenHash.Compute("session"), ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await context.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => TestSupport.Auth(context, users).LogoutAsync("session", 2));
        Assert.Null((await context.RefreshTokens.SingleAsync()).RevokedAt);
    }

    [Fact]
    public async Task FailedLoginTracksConfiguredLockout()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var user = new User { UserName = "login@test.example", Email = "login@test.example", FullName = "User", EmailConfirmed = true, LockoutEnabled = true };
        await users.CreateAsync(user, "pass123");
        var auth = TestSupport.Auth(context, users);
        for (var index = 0; index < 5; index++)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(new() { Email = user.Email, Password = "wrong" }));
        var stored = await users.FindByIdAsync(user.Id.ToString());
        Assert.True(stored!.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.False(stored.IsAdminBlocked);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(new() { Email = user.Email, Password = "pass123" }));
    }

    [Fact]
    public async Task SmtpFailureLeavesRecoverablePendingAccount()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var email = new TestEmail { Fail = true };
        var auth = TestSupport.Auth(context, users, email);
        var response = await auth.RegisterAsync(new() { Email = "pending@test.example", FullName = "User", Password = "pass123", Role = UserRole.User });
        Assert.Contains("Resend", response.Message);
        Assert.False((await users.FindByEmailAsync(response.Email))!.EmailConfirmed);
        email.Fail = false;
        await auth.ResendVerificationCodeAsync(new() { Email = response.Email });
        await auth.VerifyEmailAsync(new() { Email = response.Email, Code = email.LastCode! });
        Assert.True((await users.FindByEmailAsync(response.Email))!.EmailConfirmed);
    }

    [Fact]
    public async Task WrongOtpAttemptIsPersistedBeforeBusinessError()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        var email = new TestEmail();
        var auth = TestSupport.Auth(context, users, email);
        await auth.RegisterAsync(new() { Email = "otp@test.example", FullName = "User", Password = "pass123", Role = UserRole.User });
        var wrong = email.LastCode == "111111" ? "222222" : "111111";
        await Assert.ThrowsAsync<DomainException>(() => auth.VerifyEmailAsync(new() { Email = "otp@test.example", Code = wrong }));
        Assert.Equal(1, (await context.EmailVerificationCodes.SingleAsync()).FailedAttempts);
    }

    [Fact]
    public async Task SeederRefusesToPromoteExistingOrdinaryAccount()
    {
        using var context = TestSupport.Context();
        using var users = TestSupport.Users(context);
        await users.CreateAsync(new User { UserName = "admin@test.example", Email = "admin@test.example", FullName = "User", Role = UserRole.User });
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(users);
        using var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Admin:Email"] = "admin@test.example", ["Admin:Password"] = "test-only-password" }).Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => AdminSeeder.SeedAsync(provider, configuration));
        Assert.Equal(UserRole.User, (await users.FindByEmailAsync("admin@test.example"))!.Role);
    }

    private static CreateOrderDto OrderRequest() => new() { Items = new() { new() { ProductId = 1, Quantity = 1 } } };
}
