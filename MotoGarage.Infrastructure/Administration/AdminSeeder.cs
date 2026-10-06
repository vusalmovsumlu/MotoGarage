using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Infrastructure.Administration;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope =
            services.CreateScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<User>>();

        var email =
            configuration["Admin:Email"];

        var password =
            configuration["Admin:Password"];

        var fullName =
            configuration["Admin:FullName"]
            ?? "MotoGarage Admin";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user =
            await userManager.FindByEmailAsync(
                email);

        if (user is null)
        {
            user = new User
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var result =
                await userManager.CreateAsync(
                    user,
                    password);

            if (!result.Succeeded)
            {
                var errors =
                    string.Join(
                        " ",
                        result.Errors.Select(
                            x => x.Description));

                throw new InvalidOperationException(
                    $"Admin yaradıla bilmədi: {errors}");
            }

            return;
        }

        // Eyni email ilə user əvvəldən varsa
        // onu admin edə bilərik
        var changed = false;

        if (user.Role != UserRole.Admin)
        {
            user.Role = UserRole.Admin;
            changed = true;
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            changed = true;
        }

        if (changed)
        {
            var result =
                await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors =
                    string.Join(
                        " ",
                        result.Errors.Select(
                            x => x.Description));

                throw new InvalidOperationException(
                    $"Admin yenilənə bilmədi: {errors}");
            }
        }
    }
}