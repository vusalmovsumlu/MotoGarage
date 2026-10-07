using MotoGarage.Application.Interfaces.Services;
using System.Collections.Concurrent;

namespace MotoGarage.API.Hubs;

public class UserConnectionRegistry : IUserConnectionRegistry
{
    private readonly ConcurrentDictionary<string, (int UserId, Action Disconnect)> _connections = new();

    public void Register(int userId, string connectionId, Action disconnect) =>
        _connections[connectionId] = (userId, disconnect);

    public void Remove(int userId, string connectionId) => _connections.TryRemove(connectionId, out _);

    public void Disconnect(int userId)
    {
        foreach (var connection in _connections.Where(x => x.Value.UserId == userId))
        {
            if (_connections.TryRemove(connection.Key, out var entry)) entry.Disconnect();
        }
    }
}
