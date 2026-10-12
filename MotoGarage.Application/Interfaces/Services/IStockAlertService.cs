using MotoGarage.Domain.Entities;
namespace MotoGarage.Application.Interfaces.Services;

public interface IStockAlertService
{
    // Stage both the episode marker and notification for the caller's transaction.
    Task EvaluateAsync(Product product);
}
