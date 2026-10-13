using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Authentication;
using MotoGarage.Infrastructure.Data;
using MotoGarage.Infrastructure.Repositories;
using MotoGarage.Infrastructure.Storage;

namespace MotoGarage.Tests;

public class OrderDeliveryAndChatMediaTests
{
    [Fact]
    public async Task DeliveryIsRequiredBeforeStockMutationAndStoredAsOrderSnapshot()
    {
        using var c = TestSupport.Context();
        await TestSupport.SeedOrderData(c, 5);
        var orders = TestSupport.Orders(c);
        await Assert.ThrowsAsync<DomainException>(() => orders.CreateAsync(1, new CreateOrderDto { Items = [new() { ProductId = 1, Quantity = 1 }] }));
        Assert.Equal(5, (await c.Products.SingleAsync()).Stock);
        var request = TestSupport.Order();
        request.DeliveryPhone = "-------";
        await Assert.ThrowsAsync<DomainException>(() => orders.CreateAsync(1, request));
        request.DeliveryPhone = "+994 50 123 45 67";
        request.DeliveryAddress = "  Nizami küçəsi 10, mənzil 3  ";
        request.DeliveryNote = "  Zəng edin.  ";
        var created = await orders.CreateAsync(1, request);
        Assert.Equal("Nizami küçəsi 10, mənzil 3", created.DeliveryAddress);
        Assert.Equal("Zəng edin.", created.DeliveryNote);
        (await c.Users.FindAsync(1))!.FullName = "Changed name";
        await c.SaveChangesAsync();
        c.ChangeTracker.Clear();
        var stored = await c.Orders.SingleAsync();
        Assert.Equal("Test recipient", stored.RecipientName);
        Assert.Equal("Bakı", stored.DeliveryCity);
        Assert.Equal("+994 50 123 45 67", stored.DeliveryPhone);
        Assert.Equal(4, (await c.Products.SingleAsync()).Stock);
    }

    static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    static ChatAttachmentService Attachments(AppDbContext c, FakeMedia storage) => new(c, new TestUnitOfWork(c), new ConversationRepository(c), new AccountAccessService(c), storage, NullLogger<ChatAttachmentService>.Instance);
    static ChatService Chat(AppDbContext c, IChatAttachmentService media) => new(new ConversationRepository(c), new MessageRepository(c), new MechanicRepository(c), new StoreRepository(c), TestSupport.Mapper(), new TestUnitOfWork(c), new NotificationService(new NotificationRepository(c), TestSupport.Mapper()), media);
    static async Task SeedChat(AppDbContext c)
    {
        await TestSupport.SeedOrderData(c);
        c.Users.Add(new User { Id = 3, FullName = "Other", Role = UserRole.User, EmailConfirmed = true });
        c.Conversations.AddRange(new Conversation { Id = 10, UserId = 1, StoreId = 1 }, new Conversation { Id = 11, UserId = 3, StoreId = 1 });
        await c.SaveChangesAsync(); c.ChangeTracker.Clear();
    }

    [Fact]
    public async Task MediaOnlyMessagesAreAuthorizedAndAttachmentsCannotBeStolenReusedOrDeleted()
    {
        using var c = TestSupport.Context(); await SeedChat(c);
        var storage = new FakeMedia(); var media = Attachments(c, storage); var chat = Chat(c, media);
        var upload = await media.UploadAsync(10, 1, new MemoryStream(Png), "photo.png", "image/png");
        Assert.Equal("image", upload.Kind);
        Assert.Equal(Png, (await media.ReadAsync(upload.Id, 10, 1)).Bytes);
        await Assert.ThrowsAsync<ForbiddenException>(() => media.ReadAsync(upload.Id, 10, 2));
        await Assert.ThrowsAsync<ForbiddenException>(() => media.ReadAsync(upload.Id, 10, 3));
        await Assert.ThrowsAsync<ForbiddenException>(() => chat.SendMessageAsync(10, 2, new() { AttachmentId = upload.Id }));
        var sent = await chat.SendMessageAsync(10, 1, new() { AttachmentId = upload.Id });
        Assert.Equal("", sent.Content);
        Assert.Equal(upload.Id, sent.Attachment!.Id);
        Assert.Equal(Png, (await media.ReadAsync(upload.Id, 10, 2)).Bytes);
        await Assert.ThrowsAsync<ConflictException>(() => chat.SendMessageAsync(10, 1, new() { AttachmentId = upload.Id }));
        await Assert.ThrowsAsync<ConflictException>(() => media.DeletePendingAsync(upload.Id, 10, 1));
        Assert.Single(await chat.GetMessagesAsync(10, 2));
        Assert.Single(await c.Notifications.ToListAsync());
        Assert.Equal(upload.Id, (await chat.GetMessagesAsync(10, 2)).Single().Attachment!.Id);
    }

    [Fact]
    public async Task CrossConversationFilesAndMalformedOrOversizedFilesAreRejectedBeforeCloudUpload()
    {
        using var c = TestSupport.Context(); await SeedChat(c);
        var storage = new FakeMedia(); var media = Attachments(c, storage); var chat = Chat(c, media);
        await Assert.ThrowsAsync<ForbiddenException>(() => media.UploadAsync(10, 3, new MemoryStream(Png), "photo.png", "image/png"));
        await Assert.ThrowsAsync<DomainException>(() => media.UploadAsync(10, 1, new MemoryStream("<svg/>"u8.ToArray()), "photo.png", "image/png"));
        await Assert.ThrowsAsync<DomainException>(() => media.UploadAsync(10, 1, new MemoryStream(Png), "photo.svg", "image/svg+xml"));
        var large = new byte[ChatMediaValidation.ImageLimit + 1]; Png.CopyTo(large, 0);
        await Assert.ThrowsAsync<DomainException>(() => media.UploadAsync(10, 1, new MemoryStream(large), "photo.png", "image/png"));
        Assert.Empty(storage.Files);
        var file = await media.UploadAsync(11, 2, new MemoryStream(Png), "photo.png", "image/png");
        await Assert.ThrowsAsync<ForbiddenException>(() => chat.SendMessageAsync(10, 2, new() { Content = "Hi", AttachmentId = file.Id }));
        await Assert.ThrowsAsync<ForbiddenException>(() => media.DeletePendingAsync(file.Id, 10, 2));
        Assert.Empty(await c.Messages.ToListAsync());
    }

    [Fact]
    public async Task VideoIsSupportedAndCleanupRemovesOnlyExpiredUnsentMedia()
    {
        using var c = TestSupport.Context(); await SeedChat(c);
        var storage = new FakeMedia(); var media = Attachments(c, storage);
        var file = await media.UploadAsync(10, 1, new MemoryStream(new byte[] { 0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50 }), "clip.mp4", "video/mp4");
        Assert.Equal("video", file.Kind);
        var sent = await Chat(c, media).SendMessageAsync(10, 1, new() { Content = "Video", AttachmentId = file.Id });
        var pending = await media.UploadAsync(10, 1, new MemoryStream(Png), "photo.png", "image/png");
        foreach (var asset in await c.ChatAttachments.ToListAsync()) asset.CreatedAtUtc = DateTime.UtcNow.AddDays(-2);
        await c.SaveChangesAsync();
        await media.CleanupPendingAsync();
        Assert.Equal(file.Id, (await c.ChatAttachments.SingleAsync()).Id);
        Assert.DoesNotContain((await c.ChatAttachments.ToListAsync()), x => x.Id == pending.Id);
        Assert.Single(storage.Files);
        Assert.Equal("video", (await Chat(c, media).GetMessagesAsync(10, 2)).Single().Attachment!.Kind);
        DtoValidation.Validate(new SendMessageDto { AttachmentId = Guid.NewGuid() });
        Assert.Throws<DomainException>(() => DtoValidation.Validate(new SendMessageDto()));
    }

    internal sealed class FakeMedia : IChatMediaStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];
        public async Task<StoredChatMedia> UploadAsync(Stream stream, string fileName, string kind, CancellationToken token)
        {
            var id = Guid.NewGuid().ToString("N");
            Files[id] = await ChatMediaValidation.ReadBoundedAsync(stream, ChatMediaValidation.VideoLimit, token);
            return new(id, kind == "video" ? "mp4" : "png");
        }
        public Task<byte[]> ReadAsync(string publicId, string format, string kind, long maxBytes, CancellationToken token) => Task.FromResult(Files[publicId]);
        public Task DeleteAsync(string publicId, string kind) { Files.Remove(publicId); return Task.CompletedTask; }
    }
}
