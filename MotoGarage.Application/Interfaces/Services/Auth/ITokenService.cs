using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Services.Auth;

public interface ITokenService
{
    string CreateAccessToken(User user);

    string CreateRefreshToken();

    DateTime GetAccessTokenExpiration();

    DateTime GetRefreshTokenExpiration();
}