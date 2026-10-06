using MotoGarage.Application.DTOs.Profile;

namespace MotoGarage.Application.Interfaces.Services;

public interface IUserProfileService
{
    Task<UserProfileDto> GetMyProfileAsync(int userId);

    Task UpdateAsync(
        int userId,
        UpdateUserProfileDto dto);
}