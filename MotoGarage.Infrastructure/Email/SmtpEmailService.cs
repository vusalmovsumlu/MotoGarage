using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Application.DTOs.Vehicle;

namespace MotoGarage.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendVerificationCodeAsync(
        string email,
        string fullName,
        string code)
    {
        var message = new MimeMessage();

        message.Subject =
            "MotoGarage - Email təsdiqləmə kodu";

        message.Body =
            new BodyBuilder
            {
                HtmlBody = $"""
                    <div style="font-family:Arial,sans-serif">
                        <h2>MotoGarage</h2>

                        <p>Salam {System.Net.WebUtility.HtmlEncode(fullName)},</p>

                        <p>Email ünvanınızı təsdiqləmək üçün kod:</p>

                        <h1 style="letter-spacing:8px">
                            {code}
                        </h1>

                        <p>
                            Kod 10 dəqiqə ərzində etibarlıdır.
                        </p>

                        <p>
                            Bu sorğunu siz etməmisinizsə,
                            email-i nəzərə almayın.
                        </p>
                    </div>
                    """
            }
            .ToMessageBody();

        await SendAsync(email, message, CancellationToken.None);
    }

    public Task SendVehicleHistoryAsync(string email, string vehicleName, byte[] attachment, string fileName, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.Subject = $"MotoGarage - {vehicleName} tarixçəsi";
        var body = new BodyBuilder
        {
            TextBody = $"Salam.\n\n{vehicleName} üçün sahibin MotoGarage-dən paylaşdığı Excel hesabatı əlavə olunub. Faylda nəqliyyat vasitəsi məlumatları, servis işləri, kilometr, xərclər və yanacaq qeydləri var.\n\nHesabat sahibin daxil etdiyi qeydlərə əsaslanır."
        };
        body.Attachments.Add(fileName, attachment, ContentType.Parse(VehicleHistoryFile.ContentType));
        message.Body = body.ToMessageBody();
        return SendAsync(email, message, cancellationToken);
    }

    public Task SendAppointmentReminderAsync(string email, MotoGarage.Application.DTOs.Appointment.AppointmentReminderEmail reminder, CancellationToken cancellationToken = default) =>
        SendAsync(email, AppointmentReminderEmailBuilder.Create(reminder), cancellationToken);

    public Task SendWorkflowUpdateAsync(string email, MotoGarage.Application.DTOs.Notification.WorkflowEmailContent content, CancellationToken cancellationToken = default) =>
        SendAsync(email, new MimeMessage
        {
            MessageId = $"{content.DeliveryId:N}@motogarage.local",
            Subject = content.Subject,
            Body = new BodyBuilder { TextBody = content.TextBody, HtmlBody = content.HtmlBody }.ToMessageBody()
        }, cancellationToken);

    private async Task SendAsync(string email, MimeMessage message, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("SMTP host tapılmadı.");
        var portText = _configuration["Email:SmtpPort"] ?? throw new InvalidOperationException("SMTP port tapılmadı.");
        var username = _configuration["Email:Username"] ?? throw new InvalidOperationException("SMTP username tapılmadı.");
        var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("SMTP password tapılmadı.");
        message.From.Add(new MailboxAddress(_configuration["Email:FromName"] ?? "MotoGarage", username));
        message.To.Add(MailboxAddress.Parse(email));
        using var smtp = new SmtpClient();
        smtp.Timeout = 30000;
        await smtp.ConnectAsync(host, int.Parse(portText), SecureSocketOptions.StartTls, cancellationToken);
        await smtp.AuthenticateAsync(username, password, cancellationToken);
        await smtp.SendAsync(message, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}
