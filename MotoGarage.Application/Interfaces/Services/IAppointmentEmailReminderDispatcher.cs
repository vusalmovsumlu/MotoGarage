namespace MotoGarage.Application.Interfaces.Services;

public interface IAppointmentEmailReminderDispatcher
{
    Task<int> DispatchDueAsync(CancellationToken cancellationToken = default);
}
