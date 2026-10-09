using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.DTOs.Media;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MotoGarage.Infrastructure.Storage;

public class CloudinaryImageService : IImageService
{
    private readonly ILogger<CloudinaryImageService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(
        IConfiguration configuration,
        AppDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<CloudinaryImageService> logger)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _context = context;
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
        string fileName, int userId)
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

        var asset = new ImageAsset
        {
            PublicId = result.PublicId,
            UserId = userId,
            Url = result.SecureUrl.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        _context.ImageAssets.Add(asset);
        try { await _context.SaveChangesAsync(); }
        catch
        {
            try { await DeleteAsync(result.PublicId); }
            catch (Exception ex) { _logger.LogWarning("Upload cleanup failed: {ErrorType}", ex.GetType().Name); }
            throw;
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

    public async Task ValidateOwnershipAsync(int userId, string? publicId, string? url)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        var asset = await _context.ImageAssets.AsNoTracking().FirstOrDefaultAsync(x => x.PublicId == publicId);
        if (asset is null || asset.UserId != userId || asset.IsDeleting || asset.Url != url)
            throw new ForbiddenException("Bu şəkil sənə aid deyil və ya URL uyğun deyil.");
    }

    public async Task DeleteUnusedAsync(string? publicId, int userId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        try
        {
            await _unitOfWork.ExecuteAsync(async () =>
            {
                var asset = await _context.ImageAssets.FirstOrDefaultAsync(x => x.PublicId == publicId && x.UserId == userId);
                if (asset is null) return;
                // The write lock prevents a concurrent attachment while cleanup checks references.
                asset.IsDeleting = true;
                await _context.SaveChangesAsync();
                var used = await _context.Vehicles.AnyAsync(x => x.ImagePublicId == publicId)
                    || await _context.Mechanics.AnyAsync(x => x.ImagePublicId == publicId)
                    || await _context.Stores.AnyAsync(x => x.ImagePublicId == publicId)
                    || await _context.Products.AnyAsync(x => x.ImagePublicId == publicId)
                    || await _context.BusinessApplicationPhotos.AnyAsync(x => x.PublicId == publicId);
                if (used) asset.IsDeleting = false;
                else
                {
                    await DeleteAsync(publicId);
                    _context.ImageAssets.Remove(asset);
                }
                await _context.SaveChangesAsync();
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The business update has committed; remote cleanup failure must not report a failed update.
            _logger.LogWarning("Unused image cleanup failed: {ErrorType}", ex.GetType().Name);
        }
    }
}
