using MotoGarage.Application.Helpers;
using AutoMapper;
using MotoGarage.Application.DTOs.Review;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewRepository _reviewRepository;
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMapper _mapper;

    public ReviewService(
        IReviewRepository reviewRepository,
        IMechanicRepository mechanicRepository,
        IAppointmentRepository appointmentRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _reviewRepository = reviewRepository;
        _mechanicRepository = mechanicRepository;
        _appointmentRepository = appointmentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ReviewDto>> GetByMechanicIdAsync(
        int mechanicId)
    {
        var mechanic =
            await _mechanicRepository.GetByIdAsync(mechanicId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta tapılmadı.");
        }

        var reviews =
            await _reviewRepository
                .GetByMechanicIdAsync(mechanicId);

        return _mapper.Map<IEnumerable<ReviewDto>>(reviews);
    }

    public async Task<ReviewDto> GetByIdAsync(int id)
    {
        var review =
            await _reviewRepository.GetWithAuthorByIdAsync(id);

        if (review is null)
        {
            throw new KeyNotFoundException(
                "Rəy tapılmadı.");
        }

        return _mapper.Map<ReviewDto>(review);
    }

    public async Task<ReviewDto> CreateAsync(
        int userId,
        CreateReviewDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            ValidateRating(dto.Rating);

            var mechanic =
                await _mechanicRepository.GetByIdAsync(
                    dto.MechanicId);

            if (mechanic is null || !mechanic.IsActive)
            {
                throw new KeyNotFoundException(
                    "Usta tapılmadı.");
            }

            var hasReviewed =
                await _reviewRepository.HasUserReviewedAsync(
                    userId,
                    dto.MechanicId);

            if (hasReviewed)
            {
                throw new DomainException(
                    "Bu ustaya artıq rəy yazmısan.");
            }

            var appointments =
                await _appointmentRepository
                    .GetByUserIdAsync(userId);

            var hasCompletedAppointment =
                appointments.Any(x =>
                    x.MechanicId == dto.MechanicId &&
                    x.Status == AppointmentStatus.Completed);

            if (!hasCompletedAppointment)
            {
                throw new DomainException(
                    "Yalnız xidmət aldığın ustaya rəy yaza bilərsən.");
            }

            var review = _mapper.Map<Review>(dto);

            review.UserId = userId;
            review.CreatedAt = DateTime.UtcNow;

            await _reviewRepository.AddAsync(review);

            await _reviewRepository.SaveChangesAsync();

            await UpdateMechanicRatingAsync(mechanic);

            return await GetByIdAsync(review.Id);
        });
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateReviewDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            ValidateRating(dto.Rating);

            var review =
                await _reviewRepository.GetByIdAsync(id);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "Rəy tapılmadı.");
            }

            if (review.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu rəyi dəyişmək icazən yoxdur.");
            }

            _mapper.Map(dto, review);

            await _reviewRepository.UpdateAsync(review);

            await _reviewRepository.SaveChangesAsync();

            var mechanic =
                await _mechanicRepository.GetByIdAsync(
                    review.MechanicId);

            if (mechanic is not null)
            {
                await UpdateMechanicRatingAsync(mechanic);
            }
        });
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var review =
                await _reviewRepository.GetByIdAsync(id);

            if (review is null)
            {
                throw new KeyNotFoundException(
                    "Rəy tapılmadı.");
            }

            if (review.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu rəyi silmək icazən yoxdur.");
            }

            var mechanicId = review.MechanicId;

            await _reviewRepository.DeleteAsync(review);

            await _reviewRepository.SaveChangesAsync();

            var mechanic =
                await _mechanicRepository.GetByIdAsync(mechanicId);

            if (mechanic is not null)
            {
                await UpdateMechanicRatingAsync(mechanic);
            }
        });
    }

    private static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
        {
            throw new DomainException(
                "Reytinq 1 ilə 5 arasında olmalıdır.");
        }
    }

    private async Task UpdateMechanicRatingAsync(
        Mechanic mechanic)
    {
        mechanic.Rating =
            await _reviewRepository
                .GetAverageRatingAsync(mechanic.Id);

        await _mechanicRepository.UpdateAsync(mechanic);

        await _mechanicRepository.SaveChangesAsync();
    }
}
