using System.Text;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Helpers;

public static class ChatMediaValidation
{
    public const int ImageLimit = 5 * 1024 * 1024;
    public const int VideoLimit = 25 * 1024 * 1024;
    public static (string Kind, string ContentType) Validate(string fileName, string contentType, byte[] data)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = contentType.Split(';')[0].Trim().ToLowerInvariant();
        bool Match(byte[] prefix) => data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);
        var valid = extension switch
        {
            ".jpg" or ".jpeg" => mime == "image/jpeg" && Match([0xff, 0xd8, 0xff]),
            ".png" => mime == "image/png" && Match([137, 80, 78, 71, 13, 10, 26, 10]),
            ".webp" => mime == "image/webp" && data.Length >= 12 && Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && Encoding.ASCII.GetString(data, 8, 4) == "WEBP",
            ".mp4" => mime == "video/mp4" && data.Length >= 12 && Encoding.ASCII.GetString(data, 4, 4) == "ftyp",
            ".webm" => mime == "video/webm" && Match([0x1a, 0x45, 0xdf, 0xa3]),
            _ => false
        };
        if (!valid) throw new DomainException("JPG, PNG, WEBP şəkli və ya MP4, WEBM videosu seçin. Faylın məzmunu və formatı uyğun olmalıdır.");
        var kind = mime.StartsWith("video/") ? "video" : "image";
        if (data.Length > (kind == "image" ? ImageLimit : VideoLimit))
            throw new DomainException(kind == "image" ? "Şəkil maksimum 5 MB ola bilər." : "Video maksimum 25 MB ola bilər.");
        return (kind, mime);
    }

    public static async Task<byte[]> ReadBoundedAsync(Stream stream, long limit, CancellationToken token)
    {
        if (stream.CanSeek && stream.Length > limit) throw new DomainException("Faylın ölçüsü icazə verilən həddi keçir.");
        using var output = new MemoryStream();
        var buffer = new byte[65536];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (output.Length + count > limit) throw new DomainException("Faylın ölçüsü icazə verilən həddi keçir.");
            await output.WriteAsync(buffer.AsMemory(0, count), token);
        }
        return output.ToArray();
    }
}
