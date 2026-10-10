using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Authentication;

public sealed class AccountAccessService(AppDbContext context) : IAccountAccessService
{
    public async Task RequireParticipantAsync(int userId)
    {
        // Called inside the serializable order/appointment transaction, so deletion cannot race a new active obligation.
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
        if (user is null || user.DeletedAtUtc != null) throw new UnauthorizedAccessException("Hesab artıq aktiv deyil.");
        if (user.Role == UserRole.Admin || user.IsAdminBlocked || user.LockoutEnd > DateTimeOffset.UtcNow)
            throw new ForbiddenException("Bu hesab şəxsi sifariş və görüş yarada bilməz.");
    }
}
