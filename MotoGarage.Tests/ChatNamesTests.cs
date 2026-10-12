using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;

namespace MotoGarage.Tests;

public class ChatNamesTests
{
    static ChatService Chat(AppDbContext c) => new(new ConversationRepository(c), new MessageRepository(c),
        new MechanicRepository(c), new StoreRepository(c), TestSupport.Mapper(), new TestUnitOfWork(c),
        new NotificationService(new NotificationRepository(c), TestSupport.Mapper()));

    static async Task Seed(AppDbContext c)
    {
        await TestSupport.SeedOrderData(c);
        c.Users.Add(new User { Id = 3, FullName = "Usta Əli", Role = UserRole.Mechanic, EmailConfirmed = true });
        c.Mechanics.Add(new Mechanic { Id = 1, UserId = 3, BusinessName = "Əli Məmmədov", Slug = "eli", Description = "Service", Address = "Baku", PhoneNumber = "123", IsActive = true });
        c.Users.Find(1)!.FullName = "Vüsal Mövsümlü";
        c.Conversations.AddRange(new Conversation { Id = 10, UserId = 1, StoreId = 1 },
            new Conversation { Id = 11, UserId = 1, MechanicId = 1 });
        await c.SaveChangesAsync();
        c.ChangeTracker.Clear();
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(3, 11)]
    public async Task BusinessChatShowsCustomerNameAndNameSlug(int businessUser, int id)
    {
        using var c = TestSupport.Context();
        await Seed(c);
        var item = Assert.Single(await Chat(c).GetMyConversationsAsync(businessUser));
        Assert.Equal("Vüsal Mövsümlü", item.ParticipantName);
        Assert.Equal("Vüsal Mövsümlü", item.UserFullName);
        Assert.Equal("vusal-movsumlu", item.Slug);
        Assert.Equal(id, item.Id);
        Assert.Equal(item.Slug, (await Chat(c).GetByIdAsync(id, businessUser)).Slug);
        Assert.Empty(c.ChangeTracker.Entries<Message>());
        await Assert.ThrowsAsync<ForbiddenException>(() => Chat(c).GetByIdAsync(id, 999));
    }

    [Fact]
    public async Task CustomerSeesBusinessNamesAndNewConversationIncludesNames()
    {
        using var c = TestSupport.Context();
        await Seed(c);
        var items = (await Chat(c).GetMyConversationsAsync(1)).ToList();
        Assert.Equal("eli-memmedov", items.Single(x => x.Id == 11).Slug);
        Assert.Equal("Store", items.Single(x => x.Id == 10).ParticipantName);
        Assert.Single(await Chat(c).GetMyConversationsAsync(2));
        c.Conversations.Remove(c.Conversations.Find(11)!);
        await c.SaveChangesAsync();
        var started = await Chat(c).StartMechanicConversationAsync(1, 1);
        Assert.Equal("Vüsal Mövsümlü", started.UserFullName);
        Assert.Equal("Əli Məmmədov", started.ParticipantName);
        Assert.Equal("eli-memmedov", started.Slug);
        Assert.Equal(started.Slug, (await Chat(c).StartMechanicConversationAsync(1, 1)).Slug);
    }

    [Fact]
    public async Task EqualNamesHaveDistinctNonNumericSlugs()
    {
        using var c = TestSupport.Context();
        await Seed(c);
        c.Users.Add(new User { Id = 4, FullName = "Vüsal Mövsümlü", EmailConfirmed = true });
        c.Conversations.Add(new Conversation { Id = 12, UserId = 4, StoreId = 1 });
        await c.SaveChangesAsync();
        var items = (await Chat(c).GetMyConversationsAsync(2)).ToList();
        Assert.Equal(2, items.Select(x => x.Slug).Distinct().Count());
        Assert.All(items, x => Assert.StartsWith("vusal-movsumlu-", x.Slug));
        var again = (await Chat(c).GetMyConversationsAsync(2)).ToDictionary(x => x.Id, x => x.Slug);
        Assert.All(items, x => Assert.Equal(x.Slug, again[x.Id]));
    }
}
