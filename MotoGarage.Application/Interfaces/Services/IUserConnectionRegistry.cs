namespace MotoGarage.Application.Interfaces.Services;

public interface IUserConnectionRegistry
{
    void Register(int userId, string connectionId, Action disconnect);
    void Remove(int userId, string connectionId);
    void Disconnect(int userId);
}
