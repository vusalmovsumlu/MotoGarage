using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using MotoGarage.Application.Helpers;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Infrastructure.Storage;

public sealed class CloudinaryChatMediaStorage : IChatMediaStorage
{
    readonly Cloudinary cloudinary;
    readonly HttpClient http;
    public CloudinaryChatMediaStorage(IConfiguration config, HttpClient http)
    {
        this.http = http;
        var name = config["Cloudinary:CloudName"];
        var key = config["Cloudinary:ApiKey"];
        var secret = config["Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Cloudinary konfiqurasiyası tapılmadı.");
        cloudinary = new Cloudinary(new Account(name, key, secret));
        cloudinary.Api.Secure = true;
    }

    public async Task<StoredChatMedia> UploadAsync(Stream stream, string fileName, string kind, CancellationToken token)
    {
        var publicId = "motogarage/chat/" + Guid.NewGuid().ToString("N");
        try
        {
            UploadResult result;
            if (kind == "video")
                result = await cloudinary.UploadAsync(new VideoUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Type = "authenticated",
                    PublicId = publicId,
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false
                }, token);
            else
                result = await cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(fileName, stream),
                    Type = "authenticated",
                    PublicId = publicId,
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false
                }, token);
            if (result.Error != null || string.IsNullOrWhiteSpace(result.PublicId) || string.IsNullOrWhiteSpace(result.Format))
                throw new DomainException("Fayl Cloudinary-yə yüklənmədi. Yenidən cəhd edin.");
            return new(result.PublicId, result.Format);
        }
        catch
        {
            // Upload may succeed remotely even if its response was interrupted.
            // A server-chosen public ID lets cleanup find that exact asset.
            try { await DeleteAsync(publicId, kind); } catch { }
            throw;
        }
    }

    public async Task<byte[]> ReadAsync(string publicId, string format, string kind, long maxBytes, CancellationToken token)
    {
        var builder = kind == "video" ? cloudinary.Api.UrlVideoUp : cloudinary.Api.UrlImgUp;
        var url = builder.Type("authenticated").Secure(true).Signed(true).BuildUrl($"{publicId}.{format}");
        // The signed Cloudinary URL stays on the server; the browser receives
        // bytes only after conversation authorization and never a public URL.
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode) throw new DomainException("Chat faylını açmaq mümkün olmadı.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        return await ChatMediaValidation.ReadBoundedAsync(stream, maxBytes, token);
    }

    public async Task DeleteAsync(string publicId, string kind)
    {
        var result = await cloudinary.DestroyAsync(new DeletionParams(publicId)
        { Type = "authenticated", ResourceType = kind == "video" ? ResourceType.Video : ResourceType.Image, Invalidate = true });
        if (result.Error != null) throw new DomainException("Göndərilməmiş faylı silmək mümkün olmadı.");
    }
}
