namespace MotoGarage.Application.Interfaces.Services;

public record StoredChatMedia(string PublicId, string Format);

public interface IChatMediaStorage
{
    Task<StoredChatMedia> UploadAsync(Stream stream, string fileName, string kind, CancellationToken cancellationToken);
    Task<byte[]> ReadAsync(string publicId, string format, string kind, long maxBytes, CancellationToken cancellationToken);
    Task DeleteAsync(string publicId, string kind);
}
