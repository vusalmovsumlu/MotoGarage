using MotoGarage.Application.DTOs.Business;
namespace MotoGarage.Application.Interfaces.Services;

public interface IBusinessStatisticsService
{
    Task<BusinessStatisticsDto> GetAsync(int userId, DateOnly from, DateOnly to);
}
