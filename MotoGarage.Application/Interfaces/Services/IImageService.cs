using MotoGarage.Application.DTOs.Media;

namespace MotoGarage.Application.Interfaces.Services;

public interface IImageService
{
    Task<UploadResultDto> UploadAsync(
        Stream stream,
        string fileName, int userId);

    Task DeleteAsync(string publicId);
    Task ValidateOwnershipAsync(int userId, string? publicId, string? url);
    Task DeleteUnusedAsync(string? publicId, int userId);
}