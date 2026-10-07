using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Helpers;

public static class DtoValidation
{
    public static void Validate(object? dto)
    {
        if (dto is null) throw new DomainException("Sorğu məlumatı tələb olunur.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            throw new DomainException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
    }
}
