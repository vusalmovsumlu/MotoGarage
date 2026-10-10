namespace MotoGarage.Application.Interfaces.Services;

public interface IAccountAccessService
{
    Task RequireParticipantAsync(int userId);
}
