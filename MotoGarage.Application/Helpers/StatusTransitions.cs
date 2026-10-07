using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Helpers;

public static class StatusTransitions
{
    public static bool CanChange(OrderStatus current, OrderStatus next) => current switch
    {
        OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
        OrderStatus.Confirmed => next is OrderStatus.Preparing or OrderStatus.Cancelled,
        OrderStatus.Preparing => next is OrderStatus.Shipped or OrderStatus.Cancelled,
        OrderStatus.Shipped => next == OrderStatus.Completed,
        _ => false
    };

    public static bool CanChange(AppointmentStatus current, AppointmentStatus next) => current switch
    {
        AppointmentStatus.Pending => next is AppointmentStatus.Accepted
            or AppointmentStatus.Rejected or AppointmentStatus.Cancelled,
        AppointmentStatus.Accepted => next is AppointmentStatus.Completed or AppointmentStatus.Cancelled,
        _ => false
    };

    public static void Validate(OrderStatus current, OrderStatus next)
    {
        if (!CanChange(current, next)) throw new DomainException(
            $"Sifariş statusu {current} statusundan {next} statusuna dəyişdirilə bilməz.");
    }

    public static void Validate(AppointmentStatus current, AppointmentStatus next)
    {
        if (!CanChange(current, next)) throw new DomainException(
            $"Randevu statusu {current} statusundan {next} statusuna dəyişdirilə bilməz.");
    }
}
