using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Application.Helpers;

public sealed class VehicleYearAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is int year && year >= 1885 && year <= DateTime.UtcNow.Year + 1;

    public override string FormatErrorMessage(string name) =>
        $"{name} 1885 ilə {DateTime.UtcNow.Year + 1} arasında olmalıdır.";
}
