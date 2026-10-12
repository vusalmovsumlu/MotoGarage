namespace MotoGarage.Application.DTOs.Business;

public record PopularProductDto(int ProductId, string Name, int Quantity, decimal Amount);
public record BusinessStatisticsDto(string Role, DateTime FromUtc, DateTime ToUtc,
    int CompletedCount, int CancelledCount, int PendingCount, decimal Amount,
    double AverageRating, int ReviewCount, int LowStockCount, int OutOfStockCount,
    List<PopularProductDto> BestSellers);
