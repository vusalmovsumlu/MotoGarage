using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Administration;

public class AdminService : IAdminService
{
    private readonly IUserConnectionRegistry _connections;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public AdminService(
        AppDbContext context,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IUserConnectionRegistry connections)
    {
        _connections = connections;
        _unitOfWork = unitOfWork;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<AdminUserDto>> GetUsersAsync(
        string? search,
        UserRole? role)
    {
        var query =
            _context.Users
                .AsNoTracking()
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.FullName.Contains(search) ||
                (x.Email != null &&
                 x.Email.Contains(search)));
        }

        if (role.HasValue)
        {
            query = query.Where(x =>
                x.Role == role.Value);
        }

        var users =
            await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

        return users.Select(x =>
            new AdminUserDto
            {
                Id = x.Id,

                FullName = x.FullName,

                Email =
                    x.Email ?? string.Empty,

                Role = x.Role,

                EmailConfirmed =
                    x.EmailConfirmed,

                IsBlocked =
                    x.IsAdminBlocked || (x.LockoutEnd.HasValue &&
                    x.LockoutEnd.Value >
                    DateTimeOffset.UtcNow),

                CreatedAt = x.CreatedAt
            });
    }

    public async Task<AdminDashboardDto>
        GetDashboardAsync()
    {
        return new AdminDashboardDto
        {
            TotalUsers =
                await _context.Users.CountAsync(),

            TotalMechanics =
                await _context.Mechanics.CountAsync(
                    x => x.IsActive),

            TotalStores =
                await _context.Stores.CountAsync(
                    x => x.IsActive),

            TotalProducts =
                await _context.Products.CountAsync(
                    x => x.IsActive),

            TotalOrders =
                await _context.Orders.CountAsync(),

            PendingOrders =
                await _context.Orders.CountAsync(
                    x => x.Status ==
                         OrderStatus.Pending),

            PendingAppointments =
                await _context.Appointments.CountAsync(
                    x => x.Status ==
                         AppointmentStatus.Pending)
        };
    }

    public async Task BlockUserAsync(
        int userId,
        int currentAdminId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            if (userId == currentAdminId)
            {
                throw new DomainException(
                    "Öz hesabını bloklaya bilməzsən.");
            }

            var user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");
            }

            user.IsAdminBlocked = true;
            var enableResult =
                await _userManager.SetLockoutEnabledAsync(
                    user,
                    true);

            if (!enableResult.Succeeded)
            {
                ThrowIdentityErrors(enableResult);
            }

            var lockResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.MaxValue);

            if (!lockResult.Succeeded)
            {
                ThrowIdentityErrors(lockResult);
            }
            var tokens = await _context.RefreshTokens.Where(x =>
                x.UserId == userId && x.RevokedAt == null).ToListAsync();
            foreach (var token in tokens) token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        });
        _connections.Disconnect(userId);
    }

    public async Task UnblockUserAsync(
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");
            }

            user.IsAdminBlocked = false;
            user.AccessFailedCount = 0;
            var result =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null);

            if (!result.Succeeded)
            {
                ThrowIdentityErrors(result);
            }
        });
    }

    private static void ThrowIdentityErrors(
        IdentityResult result)
    {
        var errors =
            string.Join(
                " ",
                result.Errors.Select(
                    x => x.Description));

        throw new DomainException(errors);
    }
}