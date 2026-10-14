using System.Globalization;
using System.Net;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Infrastructure.Email;

public static class WorkflowEmailBuilder
{
    public static string AppointmentStatusText(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "Görüş müraciətiniz yaradıldı. Ustanın cavabını gözləyin.",
        AppointmentStatus.Accepted => "Görüşünüz usta tərəfindən qəbul olundu.",
        AppointmentStatus.Rejected => "Görüş müraciətiniz usta tərəfindən rədd edildi.",
        AppointmentStatus.Completed => "Görüşünüz tamamlandı.",
        AppointmentStatus.Cancelled => "Görüşünüz ləğv edildi.",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string OrderStatusText(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Sifarişiniz yaradıldı. Mağazanın təsdiqini gözləyin.",
        OrderStatus.Confirmed => "Sifarişiniz mağaza tərəfindən təsdiqləndi.",
        OrderStatus.Preparing => "Sifarişiniz hazırlanır.",
        OrderStatus.Shipped => "Sifarişiniz göndərildi.",
        OrderStatus.Completed => "Sifarişiniz tamamlandı.",
        OrderStatus.Cancelled => "Sifarişiniz ləğv edildi.",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string BakuDate(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(4)
        .ToString("dd MMMM yyyy, HH:mm", CultureInfo.GetCultureInfo("az-Latn-AZ"));

    public static (string Text, string Html) Build(string customerName, string heading, string summary,
        IEnumerable<(string Label, string Value)> details, DateTime occurredAtUtc)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        var rows = details.ToList();
        var note = $"Əməliyyat vaxtı: {BakuDate(occurredAtUtc)} (Bakı vaxtı). Son vəziyyəti MotoGarage-də yoxlaya bilərsiniz.";
        var text = $"Salam, {customerName}.\n\n{heading}\n{summary}\n\n" +
            string.Join("\n", rows.Select(x => $"{x.Label}: {x.Value}")) + $"\n\n{note}";
        var htmlRows = string.Join("", rows.Select(x => $"<tr><td style=\"padding:6px 16px 6px 0;color:#64748b\">{Encode(x.Label)}</td><td style=\"padding:6px 0\">{Encode(x.Value).Replace("\n", "<br>")}</td></tr>"));
        var html = $"""
            <div style="font-family:Arial,sans-serif;color:#202838;line-height:1.6;max-width:620px;margin:auto">
              <h2 style="color:#2878ff">MotoGarage — {Encode(heading)}</h2>
              <p>Salam, {Encode(customerName)}.</p>
              <p><strong>{Encode(summary)}</strong></p>
              <table style="width:100%;border-top:1px solid #e8eef5;border-bottom:1px solid #e8eef5">{htmlRows}</table>
              <p style="color:#64748b;font-size:13px">{Encode(note)}</p>
            </div>
            """;
        return (text, html);
    }
}
