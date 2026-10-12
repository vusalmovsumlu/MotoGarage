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
                .Where(x => x.DeletedAtUtc == null)
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
            PendingBusinessApplications = await _context.BusinessApplications.CountAsync(x => x.Status == BusinessApprovalStatus.Pending && x.User.EmailConfirmed && x.User.DeletedAtUtc == null),
            BlockedUsers = await _context.Users.CountAsync(x => x.DeletedAtUtc == null && (x.IsAdminBlocked || x.LockoutEnd > DateTimeOffset.UtcNow)),
            TotalUsers =
                await _context.Users.CountAsync(x => x.DeletedAtUtc == null),

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
        int currentAdminId, string? reason = null)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var administrator = await AdminAuditWriter.RequireAdmin(_context, currentAdminId);
            if (userId == currentAdminId)
            {
                throw new DomainException(
                    "Öz hesabını bloklaya bilməzsən.");
            }

            var user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            if (user is null || user.DeletedAtUtc != null)
            {
                throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");
            }

            if (user.IsAdminBlocked) return;
            AdminAuditWriter.Add(_context, administrator, "UserBlocked", "User", user.Id, user.FullName, AuditReason(reason, "Admin tərəfindən bloklandı."));
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
        int userId, int currentAdminId, string? reason = null)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var administrator = await AdminAuditWriter.RequireAdmin(_context, currentAdminId);
            var user =
                await _userManager.FindByIdAsync(
                    userId.ToString());

            if (user is null || user.DeletedAtUtc != null)
            {
                throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");
            }

            if (!user.IsAdminBlocked && !(user.LockoutEnd > DateTimeOffset.UtcNow)) return;
            AdminAuditWriter.Add(_context, administrator, "UserUnblocked", "User", user.Id, user.FullName, AuditReason(reason, "Blok admin tərəfindən götürüldü."));
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
            await _context.SaveChangesAsync();
        });
    }

    public async Task DeleteUserAsync(int userId, int currentAdminId, string? reason = null)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var administrator = await AdminAuditWriter.RequireAdmin(_context, currentAdminId);
            if (userId == currentAdminId)
                throw new DomainException("Öz admin hesabınızı silə bilməzsiniz.");
            var user = await _context.Users.SingleOrDefaultAsync(x => x.Id == userId)
                ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
            if (user.Role == UserRole.Admin)
                throw new DomainException("Admin hesabları bu əməliyyatla silinə bilməz.");
            if (user.DeletedAtUtc != null) return; // Repeated DELETE remains safe.

            if (await _context.Orders.AnyAsync(x => (x.UserId == userId || x.Store.UserId == userId) &&
                (x.Status == OrderStatus.Pending || x.Status == OrderStatus.Confirmed || x.Status == OrderStatus.Preparing || x.Status == OrderStatus.Shipped)) ||
                await _context.Appointments.AnyAsync(x => (x.UserId == userId || x.Mechanic.UserId == userId) &&
                    (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Accepted)))
                throw new ConflictException("Hesabın aktiv sifariş və ya görüşləri var. Əvvəl onları tamamlayın və ya ləğv edin.");

            AdminAuditWriter.Add(_context, administrator, "UserDeleted", "User", user.Id, user.FullName, AuditReason(reason, "Hesab silindi; tamamlanmış əməliyyat tarixçəsi saxlanıldı."));
            var now = DateTime.UtcNow;
            user.DeletedAtUtc = now;
            user.DeletedByAdminId = currentAdminId;
            user.IsAdminBlocked = true;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.FullName = "Silinmiş istifadəçi";
            user.Email = user.NormalizedEmail = user.PhoneNumber = user.PasswordHash = null;
            user.UserName = $"deleted-{user.Id}-{Guid.NewGuid():N}";
            user.NormalizedUserName = _userManager.NormalizeName(user.UserName);
            user.EmailConfirmed = user.PhoneNumberConfirmed = user.TwoFactorEnabled = false;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            user.BusinessApprovalStatus = BusinessApprovalStatus.Rejected;
            foreach (var token in await _context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync())
                token.RevokedAt = now;
            _context.EmailVerificationCodes.RemoveRange(await _context.EmailVerificationCodes.Where(x => x.UserId == userId).ToListAsync());
            _context.UserLogins.RemoveRange(await _context.UserLogins.Where(x => x.UserId == userId).ToListAsync());
            _context.UserTokens.RemoveRange(await _context.UserTokens.Where(x => x.UserId == userId).ToListAsync());
            _context.UserClaims.RemoveRange(await _context.UserClaims.Where(x => x.UserId == userId).ToListAsync());
            _context.UserRoles.RemoveRange(await _context.UserRoles.Where(x => x.UserId == userId).ToListAsync());
            _context.Notifications.RemoveRange(await _context.Notifications.Where(x => x.UserId == userId).ToListAsync());
            foreach (var mechanic in await _context.Mechanics.Where(x => x.UserId == userId).ToListAsync()) mechanic.IsActive = false;
            foreach (var store in await _context.Stores.Where(x => x.UserId == userId).ToListAsync()) store.IsActive = false;
            foreach (var product in await _context.Products.Where(x => x.Store.UserId == userId).ToListAsync()) product.IsActive = false;
            // Keep foreign keys and completed history; this is account deletion, not a cascade purge.
            await _context.SaveChangesAsync();
        });
        _connections.Disconnect(userId); // External action runs only after the transaction commits.
    }

    private static string AuditReason(string? reason, string fallback)
    {
        if (reason?.Length > 1000) throw new DomainException("Admin qeydi maksimum 1000 simvol ola bilər.");
        return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
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
