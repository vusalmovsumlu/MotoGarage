using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Repositories;

public class EmailVerificationCodeRepository
    : Repository<EmailVerificationCode>,
      IEmailVerificationCodeRepository
{
    private readonly AppDbContext _context;

    public EmailVerificationCodeRepository(
        AppDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<EmailVerificationCode?>
        GetLatestByUserIdAsync(int userId)
    {
        return await _context.EmailVerificationCodes
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EmailVerificationCode>>
        GetUnusedByUserIdAsync(int userId)
    {
        return await _context.EmailVerificationCodes
            .Where(x =>
                x.UserId == userId &&
                x.UsedAt == null)
            .ToListAsync();
    }
}