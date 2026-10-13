using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Storage;

public sealed class ChatAttachmentService(AppDbContext context, IUnitOfWork work, IConversationRepository conversations,
    IAccountAccessService access, IChatMediaStorage storage, ILogger<ChatAttachmentService> logger) : IChatAttachmentService
{
    async Task Authorize(int conversationId, int userId)
    {
        await access.RequireParticipantAsync(userId);
        var conversation = await conversations.GetParticipantsAsync(conversationId)
            ?? throw new KeyNotFoundException("Söhbət tapılmadı.");
        if (conversation.UserId != userId && conversation.Mechanic?.UserId != userId && conversation.Store?.UserId != userId)
            throw new ForbiddenException("Bu söhbətə giriş icazən yoxdur.");
    }

    public async Task<ChatAttachmentDto> UploadAsync(int conversationId, int userId, Stream stream, string fileName, string contentType, CancellationToken token = default)
    {
        await Authorize(conversationId, userId);
        var data = await ChatMediaValidation.ReadBoundedAsync(stream, ChatMediaValidation.VideoLimit, token);
        var (kind, mime) = ChatMediaValidation.Validate(fileName, contentType, data);
        await using var buffer = new MemoryStream(data, false);
        var stored = await storage.UploadAsync(buffer, fileName, kind, token);
        try
        {
            return await work.ExecuteAsync(async () =>
            {
                token.ThrowIfCancellationRequested();
                await Authorize(conversationId, userId);
                var safeName = Path.GetFileName(fileName.Replace('\\', '/'));
                if (safeName.Length > 200) safeName = safeName[..200];
                var asset = new ChatAttachment
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    UploadedByUserId = userId,
                    PublicId = stored.PublicId,
                    Format = stored.Format,
                    Kind = kind,
                    ContentType = mime,
                    FileName = safeName,
                    SizeBytes = data.Length,
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.ChatAttachments.Add(asset);
                await context.SaveChangesAsync(token);
                return Dto(asset);
            });
        }
        catch
        {
            try { await storage.DeleteAsync(stored.PublicId, kind); }
            catch (Exception failure) { logger.LogWarning("Chat upload cleanup failed: {ErrorType}", failure.GetType().Name); }
            throw;
        }
    }

    public async Task<ChatAttachment> ClaimAsync(Guid id, int conversationId, int userId)
    {
        await Authorize(conversationId, userId);
        var asset = await context.ChatAttachments.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Chat faylı tapılmadı.");
        if (asset.ConversationId != conversationId || asset.UploadedByUserId != userId)
            throw new ForbiddenException("Bu fayl sənə və bu söhbətə aid deyil.");
        if (asset.IsDeleting || asset.IsAttached) throw new ConflictException("Bu fayl artıq göndərilib və ya silinir.");
        asset.IsAttached = true;
        return asset;
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ReadAsync(Guid id, int conversationId, int userId, CancellationToken token = default)
    {
        await Authorize(conversationId, userId);
        var asset = await context.ChatAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ConversationId == conversationId, token)
            ?? throw new KeyNotFoundException("Chat faylı tapılmadı.");
        if (asset.IsDeleting || (!asset.IsAttached && asset.UploadedByUserId != userId) ||
            (asset.IsAttached && !await context.Messages.AnyAsync(x => x.AttachmentId == id && x.ConversationId == conversationId, token)))
            throw new ForbiddenException("Bu fayla giriş icazən yoxdur.");
        return (await storage.ReadAsync(asset.PublicId, asset.Format, asset.Kind, asset.Kind == "image" ? ChatMediaValidation.ImageLimit : ChatMediaValidation.VideoLimit, token), asset.ContentType, asset.FileName);
    }

    public async Task DeletePendingAsync(Guid id, int conversationId, int userId)
    {
        await Authorize(conversationId, userId);
        await Delete(id, userId, false, conversationId);
    }

    Task Delete(Guid id, int userId, bool expiredOnly, int? conversationId = null) => work.ExecuteAsync(async () =>
    {
        var asset = await context.ChatAttachments.SingleOrDefaultAsync(x => x.Id == id);
        if (asset == null) return;
        if (asset.UploadedByUserId != userId) throw new ForbiddenException("Bu fayl sənə aid deyil.");
        if (conversationId.HasValue && asset.ConversationId != conversationId.Value) throw new ForbiddenException("Bu fayl bu söhbətə aid deyil.");
        if (asset.IsAttached || await context.Messages.AnyAsync(x => x.AttachmentId == id))
        {
            if (expiredOnly) return;
            throw new ConflictException("Göndərilmiş chat faylını bu əməliyyatla silmək olmaz.");
        }
        if (expiredOnly && asset.CreatedAtUtc > DateTime.UtcNow.AddDays(-1)) return;
        asset.IsDeleting = true;
        await context.SaveChangesAsync();
        await storage.DeleteAsync(asset.PublicId, asset.Kind);
        context.ChatAttachments.Remove(asset);
        await context.SaveChangesAsync();
    });

    public async Task CleanupPendingAsync(CancellationToken token = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        var stale = await context.ChatAttachments.AsNoTracking().Where(x => !x.IsAttached && x.CreatedAtUtc < cutoff)
            .OrderBy(x => x.CreatedAtUtc).Take(20).Select(x => new { x.Id, x.UploadedByUserId }).ToListAsync(token);
        foreach (var asset in stale) { token.ThrowIfCancellationRequested(); await Delete(asset.Id, asset.UploadedByUserId, true); }
    }

    static ChatAttachmentDto Dto(ChatAttachment asset) => new()
    { Id = asset.Id, Kind = asset.Kind, ContentType = asset.ContentType, FileName = asset.FileName, SizeBytes = asset.SizeBytes };
}
