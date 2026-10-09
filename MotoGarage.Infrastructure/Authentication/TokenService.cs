using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MotoGarage.Application.Interfaces.Services;
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

    public TokenService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(User user)
    {
        var key =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key tapılmadı.");

        var issuer =
            _configuration["Jwt:Issuer"];

        var audience =
            _configuration["Jwt:Audience"];

        var claims =
            new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),

                new Claim(
                    ClaimTypes.Email,
                    user.Email ?? string.Empty),

                // Rol üçün vacibdir
                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()),
                new Claim("businessApprovalStatus", user.BusinessApprovalStatus.ToString())
            };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires:
                    GetAccessTokenExpiration(),
                signingCredentials:
                    credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var randomBytes =
            RandomNumberGenerator
                .GetBytes(64);

        return Convert.ToBase64String(
            randomBytes);
    }

    public DateTime GetAccessTokenExpiration()
    {
        var expireMinutes =
            int.Parse(
                _configuration[
                    "Jwt:ExpireMinutes"]
                ?? "30");

        return DateTime.UtcNow
            .AddMinutes(expireMinutes);
    }

    public DateTime GetRefreshTokenExpiration()
    {
        var expireDays =
            int.Parse(
                _configuration[
                    "Jwt:RefreshTokenExpireDays"]
                ?? "7");

        return DateTime.UtcNow
            .AddDays(expireDays);
    }
}
