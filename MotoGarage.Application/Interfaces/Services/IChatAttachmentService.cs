using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Services;

public interface IChatAttachmentService
{
    Task<ChatAttachmentDto> UploadAsync(int conversationId, int userId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ChatAttachment> ClaimAsync(Guid id, int conversationId, int userId);
    Task<(byte[] Bytes, string ContentType, string FileName)> ReadAsync(Guid id, int conversationId, int userId, CancellationToken cancellationToken = default);
    Task DeletePendingAsync(Guid id, int conversationId, int userId);
    Task CleanupPendingAsync(CancellationToken cancellationToken = default);
}
