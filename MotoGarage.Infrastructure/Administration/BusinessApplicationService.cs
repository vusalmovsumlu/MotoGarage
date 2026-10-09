using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.DTOs.Admin;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Administration;

public class BusinessApplicationService(AppDbContext context) : IBusinessApplicationService
{
    private IQueryable<BusinessApplicationDto> Query() => context.BusinessApplications.AsNoTracking()
        .Where(x => x.User.EmailConfirmed)
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new BusinessApplicationDto
        {
            Id = x.Id, FullName = x.User.FullName, Email = x.User.Email!, Role = x.User.Role,
            Status = x.Status, CreatedAt = x.CreatedAt, ReviewedAt = x.ReviewedAt,
            DecisionComment = x.DecisionComment,
            Photos = x.Photos.OrderBy(p => p.Position).Select(p => p.Url).ToList()
        });

    public async Task<BusinessApplicationDto?> GetMineAsync(int userId)
    {
        var id = await context.BusinessApplications.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        return id.HasValue ? await Query().SingleOrDefaultAsync(x => x.Id == id) : null;
    }

    public Task<List<BusinessApplicationDto>> GetApplicationsAsync(BusinessApprovalStatus? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new DomainException("Müraciət statusu düzgün deyil.");
        return (status.HasValue ? Query().Where(x => x.Status == status) : Query()).ToListAsync();
    }

    public async Task ReviewAsync(int id, int adminId, bool approve, string? comment)
    {
        comment = comment?.Trim();
        if (!approve && (string.IsNullOrWhiteSpace(comment) || comment.Length > 1000))
            throw new DomainException("Rədd etmə səbəbi məcburidir və maksimum 1000 simvol ola bilər.");
        if (!await context.Users.AnyAsync(x => x.Id == adminId && x.Role == UserRole.Admin && x.EmailConfirmed && !x.IsAdminBlocked))
            throw new ForbiddenException("Yalnız administrator müraciətə qərar verə bilər.");
        var application = await context.BusinessApplications.Include(x => x.User).Include(x => x.Photos).SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Müraciət tapılmadı.");
        if (!application.User.EmailConfirmed || application.User.IsAdminBlocked)
            throw new DomainException("Əvvəl email təsdiqlənməlidir və hesab bloklanmamış olmalıdır.");
        if (application.Status != BusinessApprovalStatus.Pending || application.User.BusinessApprovalStatus != BusinessApprovalStatus.Pending)
            throw new ConflictException("Bu müraciətə artıq qərar verilib.");
        if (application.User.Role is not (UserRole.Mechanic or UserRole.Store))
            throw new DomainException("Müraciət yalnız usta və mağaza hesabına aid ola bilər.");
        if (application.Photos.Count != 3) throw new DomainException("Müraciətdə 3 şəkil olmalıdır.");
        application.Status = approve ? BusinessApprovalStatus.Approved : BusinessApprovalStatus.Rejected;
        application.User.BusinessApprovalStatus = application.Status;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = adminId;
        application.DecisionComment = approve ? null : comment;
        context.Notifications.Add(new Notification
        {
            UserId = application.UserId, Type = NotificationType.General,
            Title = approve ? "Biznes müraciətiniz təsdiqləndi" : "Biznes müraciətiniz rədd edildi",
            Message = approve ? "Usta və ya mağaza hesabınızdan istifadə edə bilərsiniz." : comment!,
            Link = "/business-application", CreatedAt = DateTime.UtcNow
        });
        // Decision, live authorization state and notification commit together; status tokens prevent double decisions.
        await context.SaveChangesAsync();
    }
}
