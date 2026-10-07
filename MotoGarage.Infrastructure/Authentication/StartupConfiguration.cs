using Microsoft.Extensions.Configuration;
using System.Text;

namespace MotoGarage.Infrastructure.Authentication;

public static class StartupConfiguration
{
    public static string Validate(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException(
                "Jwt:Key ən azı 32 bayt olmalıdır. Development üçün User Secrets, production üçün Jwt__Key istifadə edin.");

        foreach (var name in new[] { "Jwt:Issuer", "Jwt:Audience", "Cloudinary:CloudName",
                     "Cloudinary:ApiKey", "Cloudinary:ApiSecret", "Email:SmtpHost",
                     "Email:Username", "Email:Password", "Google:ClientId" })
        {
            if (string.IsNullOrWhiteSpace(configuration[name]))
                throw new InvalidOperationException($"{name} konfiqurasiyası tələb olunur.");
        }

        if (!int.TryParse(configuration["Email:SmtpPort"], out var port) || port < 1 || port > 65535)
            throw new InvalidOperationException("Email:SmtpPort düzgün port olmalıdır.");
        foreach (var name in new[] { "Jwt:ExpireMinutes", "Jwt:RefreshTokenExpireDays" })
        {
            if (configuration[name] is not null &&
                (!int.TryParse(configuration[name], out var value) || value <= 0))
                throw new InvalidOperationException($"{name} müsbət tam ədəd olmalıdır.");
        }
        return key;
    }
}
