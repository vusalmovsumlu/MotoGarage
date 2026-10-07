using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Helpers;

public static class DateRange
{
    // Public dates are inclusive calendar days; SQL uses [start, next-day-exclusive).
    public static (DateTime Start, DateTime EndExclusive) Days(DateTime start, DateTime end)
    {
        if (start > end || end.Date == DateTime.MaxValue.Date)
            throw new DomainException("Tarix aralığı düzgün deyil.");
        return (start.Date, end.Date.AddDays(1));
    }
}
