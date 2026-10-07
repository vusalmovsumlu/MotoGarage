namespace MotoGarage.Application.DTOs.Order;

public class CreateOrderDto : System.ComponentModel.DataAnnotations.IValidatableObject
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(1), System.ComponentModel.DataAnnotations.MaxLength(100)]
    public List<CreateOrderItemDto> Items { get; set; }
        = new();
    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(
        System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        if (Items is null) yield break;
        for (var index = 0; index < Items.Count; index++)
        {
            if (Items[index] is null)
            {
                yield return new("Sifariş elementi null ola bilməz.", new[] { $"Items[{index}]" });
                continue;
            }
            var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(Items[index],
                new System.ComponentModel.DataAnnotations.ValidationContext(Items[index]), errors, true);
            foreach (var error in errors) yield return error;
        }
    }
}