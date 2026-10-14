namespace MotoGarage.Application.Interfaces.Services;

public interface IWorkflowEmailQueue
{
    Task AppointmentAsync(int appointmentId, bool timeChanged = false);
    Task OrderAsync(int orderId);
}
