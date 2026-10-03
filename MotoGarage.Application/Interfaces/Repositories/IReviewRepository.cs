using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Interfaces.Repository;

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetByMechanicIdAsync(int mechanicId);

    Task<double> GetAverageRatingAsync(int mechanicId);

    Task<bool> HasUserReviewedAsync(
        int userId,
        int mechanicId);
}