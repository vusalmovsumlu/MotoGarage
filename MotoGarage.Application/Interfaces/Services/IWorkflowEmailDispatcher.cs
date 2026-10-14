namespace MotoGarage.Application.Interfaces.Services;

public interface IWorkflowEmailDispatcher
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken = default);
}
