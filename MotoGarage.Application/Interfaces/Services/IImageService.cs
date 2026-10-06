using MotoGarage.Application.DTOs.Media;

namespace MotoGarage.Application.Interfaces.Services;

public interface IImageService
{
    Task<UploadResultDto> UploadAsync(
        Stream stream,
        string fileName);

    Task DeleteAsync(string publicId);
}