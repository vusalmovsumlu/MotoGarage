using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Infrastructure.Data;

namespace MotoGarage.Infrastructure.Email;

public sealed class WorkflowEmailQueue(AppDbContext context, TimeProvider clock) : IWorkflowEmailQueue
{
    public async Task AppointmentAsync(int appointmentId, bool timeChanged = false)
    {
        var item = await context.Appointments.AsNoTracking().Where(x => x.Id == appointmentId).Select(x => new
        {
            x.UserId, x.Status, x.AppointmentDate, x.RescheduleRevision,
            x.User.FullName, x.User.Email, x.User.EmailConfirmed, x.User.IsAdminBlocked, x.User.DeletedAtUtc,
            Vehicle = x.Vehicle.Brand + " " + x.Vehicle.Model,
            x.Mechanic.BusinessName, MechanicName = x.Mechanic.User.FullName, x.Mechanic.Address
        }).SingleAsync();
        if (!Eligible(item.Email, item.EmailConfirmed, item.IsAdminBlocked, item.DeletedAtUtc)) return;
        var key = timeChanged ? $"appointment:{appointmentId}:time:{item.RescheduleRevision}"
            : $"appointment:{appointmentId}:status:{(int)item.Status}";
        var heading = timeChanged ? "Görüş vaxtı dəyişdi" : "Görüş statusu";
        var now = clock.GetUtcNow().UtcDateTime;
        var body = WorkflowEmailBuilder.Build(item.FullName, heading,
            timeChanged ? "Görüşünüzün yeni vaxtı qarşılıqlı razılıqla təsdiqləndi." : WorkflowEmailBuilder.AppointmentStatusText(item.Status),
            [("Görüş", $"#{appointmentId}"), ("Tarix və saat", WorkflowEmailBuilder.BakuDate(item.AppointmentDate) + " (Bakı vaxtı)"),
             ("Nəqliyyat vasitəsi", item.Vehicle), ("Biznes", item.BusinessName),
             ("Usta", string.IsNullOrWhiteSpace(item.MechanicName) ? item.BusinessName : item.MechanicName), ("Ünvan", item.Address)], now);
        await Add(key, item.UserId, $"MotoGarage — {heading} #{appointmentId}", body, now);
    }

    public async Task OrderAsync(int orderId)
    {
        var item = await context.Orders.AsNoTracking().Where(x => x.Id == orderId).Select(x => new
        {
            x.UserId, x.Status, x.TotalAmount, x.DeliveryCity, x.DeliveryAddress,
            x.User.FullName, x.User.Email, x.User.EmailConfirmed, x.User.IsAdminBlocked, x.User.DeletedAtUtc,
            x.Store.StoreName,
            Items = x.OrderItems.OrderBy(i => i.Id).Select(i => new { i.Product.Name, i.Quantity }).ToList()
        }).SingleAsync();
        if (!Eligible(item.Email, item.EmailConfirmed, item.IsAdminBlocked, item.DeletedAtUtc)) return;
        var now = clock.GetUtcNow().UtcDateTime;
        var details = new List<(string Label, string Value)>
        {
            ("Sifariş", $"#{orderId}"), ("Mağaza", item.StoreName),
            ("Məhsullar", string.Join("\n", item.Items.Select(x => $"{x.Name} × {x.Quantity}"))),
            ("Ümumi məbləğ", item.TotalAmount.ToString("0.00", CultureInfo.GetCultureInfo("az-Latn-AZ")) + " ₼")
        };
        if (!string.IsNullOrWhiteSpace(item.DeliveryAddress))
            details.Add(("Çatdırılma ünvanı", string.Join(", ", new[] { item.DeliveryCity, item.DeliveryAddress }.Where(x => !string.IsNullOrWhiteSpace(x)))));
        var body = WorkflowEmailBuilder.Build(item.FullName, "Sifariş statusu", WorkflowEmailBuilder.OrderStatusText(item.Status), details, now);
        await Add($"order:{orderId}:status:{(int)item.Status}", item.UserId, $"MotoGarage — Sifariş statusu #{orderId}", body, now);
    }

    private static bool Eligible(string? email, bool confirmed, bool blocked, DateTime? deletedAt) =>
        confirmed && !blocked && deletedAt == null && !string.IsNullOrWhiteSpace(email);

    private async Task Add(string eventKey, int recipient, string subject, (string Text, string Html) body, DateTime now)
    {
        if (await context.WorkflowEmails.AnyAsync(x => x.EventKey == eventKey)) return;
        context.WorkflowEmails.Add(new WorkflowEmail
        {
            EventKey = eventKey, RecipientUserId = recipient, Subject = subject,
            TextBody = body.Text, HtmlBody = body.Html, CreatedAtUtc = now
        });
        // Uses the caller's UnitOfWork transaction, including notification/stock changes.
        await context.SaveChangesAsync();
    }
}
