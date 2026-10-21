using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Tests;

public class MultiStoreCheckoutTests
{
    [Fact]
    public async Task OneCheckoutCreatesSeparatePricedOrdersAndMessagesForThreeStores()
    {
        await using var context = TestSupport.Context();
        var data = await SeedAsync(context);
        var request = Cart(data);
        request.Items.Reverse();
        var result = await TestSupport.Orders(context).CheckoutAsync(data.Customer.Id, request);

        Assert.Equal(3, result.Orders.Count);
        Assert.Equal(61.75m, result.TotalAmount);
        Assert.Equal(new[] { 25.25m, 20m, 16.50m }, result.Orders.Select(x => x.TotalAmount));
        Assert.All(result.Orders, order =>
        {
            Assert.Equal(data.Customer.Id, order.UserId);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal("Test recipient", order.RecipientName);
            Assert.Equal("Test küçəsi 10, mənzil 2", order.DeliveryAddress);
            Assert.Equal(data.Stores.Single(x => x.Id == order.StoreId).StoreName, order.StoreName);
            Assert.All(order.Items, item =>
            {
                var product = data.Products.Single(x => x.Id == item.ProductId);
                Assert.Equal(order.StoreId, product.StoreId);
                Assert.Equal(product.Price, item.UnitPrice);
                Assert.Equal(product.Name, item.ProductName);
            });
        });
        Assert.Equal(new[] { 8, 9, 9, 7 }, await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync());
        Assert.Equal(3, await context.WorkflowEmails.CountAsync());
        Assert.All(await context.WorkflowEmails.ToListAsync(), email => Assert.Equal(data.Customer.Id, email.RecipientUserId));
        var notifications = await context.Notifications.Where(x => x.Type == NotificationType.Order).ToListAsync();
        Assert.Equal(3, notifications.Count);
        foreach (var order in result.Orders)
        {
            var seller = data.Stores.Single(x => x.Id == order.StoreId);
            Assert.Single(notifications.Where(x => x.UserId == seller.UserId && x.Link == $"/orders/{order.Id}"));
            var email = await context.WorkflowEmails.SingleAsync(x => x.EventKey == $"order:{order.Id}:status:1");
            Assert.Contains(seller.StoreName, email.TextBody);
            Assert.All(data.Products.Where(x => x.StoreId != seller.Id), product => Assert.DoesNotContain(product.Name, email.TextBody));
        }
    }

    [Fact]
    public async Task StoreOwnershipStatusesAndCancellationRemainIndependentAfterCheckout()
    {
        await using var context = TestSupport.Context();
        var data = await SeedAsync(context);
        var service = TestSupport.Orders(context);
        var result = await service.CheckoutAsync(data.Customer.Id, Cart(data));
        Assert.Equal(3, (await service.GetMyOrdersAsync(data.Customer.Id)).Count());
        foreach (var store in data.Stores)
        {
            var own = Assert.Single(await service.GetStoreOrdersAsync(store.UserId));
            Assert.Equal(store.Id, own.StoreId);
        }
        var first = result.Orders[0];
        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(first.Id, data.Stores[1].UserId));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateStatusAsync(first.Id, data.Stores[1].UserId, new() { Status = OrderStatus.Confirmed }));
        await service.UpdateStatusAsync(first.Id, data.Stores[0].UserId, new() { Status = OrderStatus.Confirmed });
        await service.CancelAsync(first.Id, data.Customer.Id);
        await service.CancelAsync(first.Id, data.Customer.Id);
        Assert.Equal(OrderStatus.Cancelled, (await service.GetByIdAsync(first.Id, data.Customer.Id)).Status);
        Assert.All((await service.GetMyOrdersAsync(data.Customer.Id)).Where(x => x.Id != first.Id), order => Assert.Equal(OrderStatus.Pending, order.Status));
        Assert.Equal(new[] { 10, 10, 9, 7 }, await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync());
    }

    [Theory]
    [InlineData("stock")]
    [InlineData("inactiveProduct")]
    [InlineData("inactiveStore")]
    [InlineData("blockedSeller")]
    [InlineData("deletedSeller")]
    [InlineData("blockedBuyer")]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("empty")]
    [InlineData("invalidQuantity")]
    [InlineData("invalidAddress")]
    [InlineData("nullItem")]
    [InlineData("tooManyItems")]
    public async Task InvalidCartIsRejectedBeforeAnyStoreOrderOrStockChange(string condition)
    {
        await using var context = TestSupport.Context();
        var data = await SeedAsync(context);
        var request = Cart(data);
        if (condition == "stock") (await context.Products.FindAsync(data.Products[^1].Id))!.Stock = 0;
        if (condition == "inactiveProduct") (await context.Products.FindAsync(data.Products[^1].Id))!.IsActive = false;
        if (condition == "inactiveStore") (await context.Stores.FindAsync(data.Stores[^1].Id))!.IsActive = false;
        if (condition == "blockedSeller") (await context.Users.FindAsync(data.Stores[^1].UserId))!.IsAdminBlocked = true;
        if (condition == "deletedSeller") (await context.Users.FindAsync(data.Stores[^1].UserId))!.DeletedAtUtc = DateTime.UtcNow;
        if (condition == "blockedBuyer") (await context.Users.FindAsync(data.Customer.Id))!.IsAdminBlocked = true;
        if (condition == "missing") request.Items[^1].ProductId = int.MaxValue;
        if (condition == "duplicate") request.Items.Add(new() { ProductId = request.Items[0].ProductId, Quantity = 1 });
        if (condition == "empty") request.Items.Clear();
        if (condition == "invalidQuantity") request.Items[^1].Quantity = 0;
        if (condition == "invalidAddress") request.DeliveryAddress = " ";
        if (condition == "nullItem") request.Items.Add(null!);
        if (condition == "tooManyItems") request.Items.AddRange(Enumerable.Range(0, 100).Select(_ => new CreateOrderItemDto { ProductId = 1, Quantity = 1 }));
        await context.SaveChangesAsync();
        var stocks = await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync();

        var failure = await Record.ExceptionAsync(() => TestSupport.Orders(context).CheckoutAsync(data.Customer.Id, request));
        if (condition is "blockedBuyer" or "blockedSeller") Assert.IsType<ForbiddenException>(failure);
        else if (condition == "deletedSeller") Assert.IsType<UnauthorizedAccessException>(failure);
        else if (condition is "missing" or "inactiveProduct") Assert.IsType<KeyNotFoundException>(failure);
        else Assert.IsType<DomainException>(failure);
        context.ChangeTracker.Clear();
        Assert.Equal(stocks, await context.Products.OrderBy(x => x.Id).Select(x => x.Stock).ToArrayAsync());
        Assert.Empty(await context.Orders.ToListAsync());
        Assert.Empty(await context.Notifications.ToListAsync());
        Assert.Empty(await context.WorkflowEmails.ToListAsync());
    }

    [Fact]
    public async Task ExistingSingleOrderEndpointStillRejectsMixedStoresAndReturnsOneOrder()
    {
        await using var context = TestSupport.Context();
        var data = await SeedAsync(context);
        var service = TestSupport.Orders(context);
        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(data.Customer.Id, Cart(data)));
        Assert.Empty(context.Orders);
        var single = await service.CreateAsync(data.Customer.Id, TestSupport.Order(data.Products[0].Id));
        Assert.Equal(data.Stores[0].Id, single.StoreId);
        Assert.Single(single.Items);
    }

    internal sealed record CheckoutData(User Customer, Store[] Stores, Product[] Products);

    internal static async Task<CheckoutData> SeedAsync(AppDbContext context)
    {
        var customer = new User { FullName = "Buyer", Email = "buyer@example.invalid", EmailConfirmed = true, Role = UserRole.User };
        context.Users.Add(customer);
        var stores = Enumerable.Range(0, 3).Select(index => new Store
        {
            User = new User { FullName = $"Seller {index}", EmailConfirmed = true, Role = UserRole.Store, BusinessApprovalStatus = BusinessApprovalStatus.Approved },
            StoreName = $"Shop {index}", Slug = $"shop-{index}", Address = "Baku", Description = "Parts", PhoneNumber = "123", IsActive = true
        }).ToArray();
        var prices = new[] { 10.50m, 4.25m, 20m, 5.50m };
        var products = prices.Select((price, index) => new Product
        {
            Store = stores[index < 2 ? 0 : index - 1], Name = $"Product {index}", Slug = $"product-{index}",
            Price = price, Stock = 10, LowStockThreshold = 1, IsActive = true
        }).ToArray();
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return new(customer, stores, products);
    }

    internal static CreateOrderDto Cart(CheckoutData data)
    {
        var request = TestSupport.Order(data.Products[0].Id);
        request.RecipientName = " Test recipient ";
        request.DeliveryAddress = " Test küçəsi 10, mənzil 2 ";
        var quantities = new[] { 2, 1, 1, 3 };
        request.Items = data.Products.Select((product, index) => new CreateOrderItemDto { ProductId = product.Id, Quantity = quantities[index] }).ToList();
        return request;
    }
}
