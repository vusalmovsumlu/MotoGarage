using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MotoGarage.Application.Interfaces.Services.Auth;
using MotoGarage.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MotoGarage.Infrastructure.Authentication;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key tapılmadı.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var expireMinutes = int.Parse(
            _configuration["Jwt:ExpireMinutes"]
            ?? throw new InvalidOperationException(
                "JWT ExpireMinutes tapılmadı.")
        );

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()
            ),

            new(
                ClaimTypes.Name,
                user.FullName
            ),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty
            ),

            new(
                ClaimTypes.Role,
                user.Role.ToString()
            )
        };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var randomBytes = new byte[64];

        using var randomNumberGenerator =
            RandomNumberGenerator.Create();

        randomNumberGenerator.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetAccessTokenExpiration()
    {
        var expireMinutes = int.Parse(
            _configuration["Jwt:ExpireMinutes"]
            ?? throw new InvalidOperationException(
                "JWT ExpireMinutes tapılmadı.")
        );

        return DateTime.UtcNow.AddMinutes(expireMinutes);
    }

    public DateTime GetRefreshTokenExpiration()
    {
        var expireDays = int.Parse(
            _configuration["Jwt:RefreshTokenExpireDays"]
            ?? throw new InvalidOperationException(
                "RefreshTokenExpireDays tapılmadı.")
        );

        return DateTime.UtcNow.AddDays(expireDays);
    }
}