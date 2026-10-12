using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Administration;

internal static class AdminAuditWriter
{
    public static async Task<User> RequireAdmin(AppDbContext context, int id)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (user is null || user.Role != UserRole.Admin || !user.EmailConfirmed || user.DeletedAtUtc != null ||
            user.IsAdminBlocked || user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new ForbiddenException("Yalnız aktiv administrator bu əməliyyatı edə bilər.");
        return user;
    }
    public static void Add(AppDbContext context, User admin, string action, string targetType, int targetId, string name, string? reason) =>
        context.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminId = admin.Id,
            AdminName = admin.FullName,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetName = name,
            Reason = reason,
            OccurredAtUtc = DateTime.UtcNow
        });
}

public sealed class AdminAuditService(AppDbContext context) : IAdminAuditService
{
    public async Task<AdminAuditPage> GetAsync(int adminId, int page, int pageSize, string? action)
    {
        await AdminAuditWriter.RequireAdmin(context, adminId);
        if (page < 1 || page > 1000000 || pageSize < 1 || pageSize > 100) throw new DomainException("Səhifələmə düzgün deyil.");
        var query = context.AdminAuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            if (!new[] { "UserBlocked", "UserUnblocked", "UserDeleted", "ApplicationApproved", "ApplicationRejected" }.Contains(action))
                throw new DomainException("Əməliyyat filteri düzgün deyil.");
            query = query.Where(x => x.Action == action);
        }
        var count = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AdminAuditDto(x.Id, x.AdminId, x.AdminName,
                x.Action, x.TargetType, x.TargetId, x.TargetName, x.Reason, x.OccurredAtUtc)).ToListAsync();
        return new(count, page, pageSize, items);
    }
}
