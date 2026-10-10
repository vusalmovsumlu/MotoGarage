using System.Globalization;
using System.Net;
using MimeKit;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Infrastructure.Email;

public static class AppointmentReminderEmailBuilder
{
    public static MimeMessage Create(AppointmentReminderEmail reminder)
    {
        var localDate = DateTime.SpecifyKind(reminder.AppointmentDateUtc, DateTimeKind.Utc).AddHours(4);
        var date = localDate.ToString("dd MMMM yyyy, HH:mm", CultureInfo.GetCultureInfo("az-Latn-AZ"));
        var status = reminder.Status == AppointmentStatus.Pending
            ? "Gözləmədə — usta hələ görüşü təsdiqləməyib."
            : "Usta tərəfindən qəbul edilib.";
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        return new MimeMessage
        {
            Subject = "MotoGarage — Görüş xatırlatması",
            Body = new BodyBuilder
            {
                TextBody = $"Salam, {reminder.CustomerName}.\n\nSizin {date} tarixində (Bakı vaxtı) görüşünüz var.\nNəqliyyat vasitəsi: {reminder.VehicleName}\nUsta: {reminder.MechanicName}\nÜnvan: {reminder.Address}\nStatus: {status}\n\nGörüş planınızı MotoGarage-də yoxlaya bilərsiniz.",
                HtmlBody = $"""
                    <div style="font-family:Arial,sans-serif;color:#202838;line-height:1.6">
                      <h2>MotoGarage — Görüş xatırlatması</h2>
                      <p>Salam, {Encode(reminder.CustomerName)}.</p>
                      <p>Sizin <strong>{Encode(date)}</strong> tarixində (Bakı vaxtı) görüşünüz var.</p>
                      <p><strong>Nəqliyyat vasitəsi:</strong> {Encode(reminder.VehicleName)}<br>
                      <strong>Usta:</strong> {Encode(reminder.MechanicName)}<br>
                      <strong>Ünvan:</strong> {Encode(reminder.Address)}<br>
                      <strong>Status:</strong> {Encode(status)}</p>
                      <p>Görüş planınızı MotoGarage-də yoxlaya bilərsiniz.</p>
                    </div>
                    """
            }.ToMessageBody()
        };
    }
}
