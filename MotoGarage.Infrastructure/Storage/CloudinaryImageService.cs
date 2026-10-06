using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.DTOs.Media;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Infrastructure.Storage;

public class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(
        IConfiguration configuration)
    {
        var cloudName =
            configuration["Cloudinary:CloudName"];

        var apiKey =
            configuration["Cloudinary:ApiKey"];

        var apiSecret =
            configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary konfiqurasiyası tapılmadı.");
        }

        var account = new Account(
            cloudName,
            apiKey,
            apiSecret);

        _cloudinary = new Cloudinary(account);

        _cloudinary.Api.Secure = true;
    }

    public async Task<UploadResultDto> UploadAsync(
        Stream stream,
        string fileName)
    {
        if (stream is null)
        {
            throw new DomainException(
                "Şəkil seçilməyib.");
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            throw new DomainException(
                "Şəkil boşdur.");
        }

        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!allowedExtensions.Contains(extension))
        {
            throw new DomainException(
                "Yalnız JPG, JPEG, PNG və WEBP formatları qəbul edilir.");
        }

        const long maxFileSize =
            5 * 1024 * 1024;

        if (stream.CanSeek &&
            stream.Length > maxFileSize)
        {
            throw new DomainException(
                "Şəklin ölçüsü maksimum 5 MB ola bilər.");
        }

        var uploadParams =
            new ImageUploadParams
            {
                File = new FileDescription(
                    fileName,
                    stream),

                Folder = "motogarage",

                UseFilename = false,

                UniqueFilename = true,

                Overwrite = false
            };

        var result =
            await _cloudinary.UploadAsync(
                uploadParams);

        if (result.Error is not null)
        {
            throw new DomainException(
                $"Şəkil yüklənmədi: {result.Error.Message}");
        }

        if (result.SecureUrl is null ||
            string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new DomainException(
                "Cloudinary şəkli yükləyə bilmədi.");
        }

        return new UploadResultDto
        {
            Url = result.SecureUrl.ToString(),
            PublicId = result.PublicId
        };
    }

    public async Task DeleteAsync(
        string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var deleteParams =
            new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

        var result =
            await _cloudinary.DestroyAsync(
                deleteParams);

        if (result.Error is not null)
        {
            throw new DomainException(
                $"Şəkil silinə bilmədi: {result.Error.Message}");
        }
    }
}