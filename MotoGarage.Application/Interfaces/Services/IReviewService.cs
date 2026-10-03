using MotoGarage.Application.DTOs.Review;

namespace MotoGarage.Application.Interfaces.Services;

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetByMechanicIdAsync(
        int mechanicId);

    Task<ReviewDto> GetByIdAsync(int id);

    Task<ReviewDto> CreateAsync(
        int userId,
        CreateReviewDto dto);

    Task UpdateAsync(
        int id,
        int userId,
        UpdateReviewDto dto);

    Task DeleteAsync(
        int id,
        int userId);
}