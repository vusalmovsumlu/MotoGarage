using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repositories;

public interface IEmailVerificationCodeRepository
    : IRepository<EmailVerificationCode>
{
    Task<EmailVerificationCode?> GetLatestByUserIdAsync(
        int userId);

    Task<IEnumerable<EmailVerificationCode>>
        GetUnusedByUserIdAsync(int userId);
}