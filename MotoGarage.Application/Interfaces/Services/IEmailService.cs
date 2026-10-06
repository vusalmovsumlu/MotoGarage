namespace MotoGarage.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendVerificationCodeAsync(
        string email,
        string fullName,
        string code);
}