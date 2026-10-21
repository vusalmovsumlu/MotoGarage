using System.Globalization;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Infrastructure.Email;

internal static class ReminderEmailBuilder
{
    private const string Prefix = "reminder:";

    public static string EventKey(Reminder reminder) => $"{Prefix}{reminder.Id}:due:{reminder.ScheduleRevision}";

    public static bool IsReminder(string eventKey) => eventKey.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool TryReadSource(string eventKey, out int id, out int revision)
    {
        id = revision = 0;
        var parts = eventKey.Split(':');
        return parts.Length == 4 && parts[0] == "reminder" && parts[2] == "due"
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0
            && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out revision) && revision >= 0;
    }

    public static WorkflowEmail Build(Reminder reminder, int userId, string fullName,
        string vehicleName, string plateNumber, int currentKm, DateTime now)
    {
        var dateDue = reminder.ReminderDate <= now;
        var kmDue = reminder.ReminderKm.HasValue && currentKm >= reminder.ReminderKm.Value;
        var summary = dateDue && kmDue ? "Xatırlatmanızın tarixi və odometr həddi çatıb."
            : dateDue ? "Xatırlatmanızın planlaşdırılan tarixi çatıb." : "Xatırlatmanızın odometr həddi çatıb.";
        var details = new List<(string Label, string Value)>
        {
            ("Xatırlatma", reminder.Title),
            ("Nəqliyyat vasitəsi", $"{vehicleName} ({plateNumber})"),
            ("Planlaşdırılan tarix", WorkflowEmailBuilder.BakuDate(reminder.ReminderDate) + " (Bakı vaxtı)"),
            ("Cari odometr", currentKm.ToString("N0", CultureInfo.GetCultureInfo("az-Latn-AZ")) + " km")
        };
        if (reminder.ReminderKm.HasValue)
            details.Add(("Odometr həddi", reminder.ReminderKm.Value.ToString("N0", CultureInfo.GetCultureInfo("az-Latn-AZ")) + " km"));
        if (!string.IsNullOrWhiteSpace(reminder.Description)) details.Add(("Qeyd", reminder.Description));
        var body = WorkflowEmailBuilder.Build(fullName, "Xatırlatma", summary, details, now);
        return new WorkflowEmail
        {
            EventKey = EventKey(reminder), RecipientUserId = userId,
            Subject = $"MotoGarage — Xatırlatma: {reminder.Title}",
            TextBody = body.Text, HtmlBody = body.Html, CreatedAtUtc = now
        };
    }
}
