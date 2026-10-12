using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Inventory;

public sealed class StockAlertService(AppDbContext context) : IStockAlertService
{
    public async Task EvaluateAsync(Product product)
    {
        var low = product.IsActive && product.Stock <= product.LowStockThreshold;
        if (!low) { product.LowStockAlertActive = false; return; }
        if (product.LowStockAlertActive) return;
        var ownerId = await context.Stores.Where(x => x.Id == product.StoreId && x.IsActive &&
            x.User.DeletedAtUtc == null && !x.User.IsAdminBlocked).Select(x => (int?)x.UserId).SingleOrDefaultAsync();
        if (!ownerId.HasValue) return;
        product.LowStockAlertActive = true;
        context.Notifications.Add(new Notification
        {
            UserId = ownerId.Value,
            Type = NotificationType.General,
            Title = product.Stock == 0 ? "Məhsulun stoku bitdi" : "Stok azalır",
            Message = $"{product.Name}: {product.Stock} ədəd qalıb. Xəbərdarlıq həddi: {product.LowStockThreshold}.",
            Link = product.Stock == 0 ? "/inventory?stock=out" : "/inventory?stock=low",
            CreatedAt = DateTime.UtcNow
        });
    }
}
