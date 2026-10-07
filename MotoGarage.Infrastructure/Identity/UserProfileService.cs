using MotoGarage.Application.Helpers;
using Microsoft.AspNetCore.Identity;
using MotoGarage.Application.DTOs.Profile;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Infrastructure.Identity;

public class UserProfileService : IUserProfileService
{
    private readonly UserManager<User> _userManager;

    public UserProfileService(
        UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserProfileDto> GetMyProfileAsync(
        int userId)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");
        }

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task UpdateAsync(
        int userId,
        UpdateUserProfileDto dto)
    {
        DtoValidation.Validate(dto);
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            throw new DomainException(
                "Ad və soyad boş ola bilməz.");
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber =
            string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null
                : dto.PhoneNumber.Trim();

        var result =
            await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                " ",
                result.Errors.Select(
                    x => x.Description));

            throw new DomainException(errors);
        }
    }
}