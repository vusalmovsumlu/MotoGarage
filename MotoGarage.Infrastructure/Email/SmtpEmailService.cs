using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MotoGarage.Application.Interfaces.Services;

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
        var host =
            _configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException(
                "SMTP host tapılmadı.");

        var portText =
            _configuration["Email:SmtpPort"]
            ?? throw new InvalidOperationException(
                "SMTP port tapılmadı.");

        var username =
            _configuration["Email:Username"]
            ?? throw new InvalidOperationException(
                "SMTP username tapılmadı.");

        var password =
            _configuration["Email:Password"]
            ?? throw new InvalidOperationException(
                "SMTP password tapılmadı.");

        var fromName =
            _configuration["Email:FromName"]
            ?? "MotoGarage";

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                username));

        message.To.Add(
            MailboxAddress.Parse(email));

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

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            host,
            int.Parse(portText),
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            username,
            password);

        await smtp.SendAsync(message);

        await smtp.DisconnectAsync(true);
    }
}