namespace MotoGarage.Application.Interfaces.Services;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    Task ExecuteAsync(Func<Task> action);
}
