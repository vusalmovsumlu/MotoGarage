namespace MotoGarage.Application.DTOs.Order;

public class CreateOrderDto : System.ComponentModel.DataAnnotations.IValidatableObject
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Alıcının adı tələb olunur."), System.ComponentModel.DataAnnotations.StringLength(150)]
    public string RecipientName { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Çatdırılma telefonu tələb olunur."), System.ComponentModel.DataAnnotations.StringLength(30), System.ComponentModel.DataAnnotations.RegularExpression(@"\+?[0-9 ()-]{7,30}", ErrorMessage = "Düzgün telefon nömrəsi yazın.")]
    public string DeliveryPhone { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Şəhər tələb olunur."), System.ComponentModel.DataAnnotations.StringLength(100)]
    public string DeliveryCity { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Tam çatdırılma ünvanı tələb olunur."), System.ComponentModel.DataAnnotations.StringLength(500)]
    public string DeliveryAddress { get; set; } = "";
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? DeliveryNote { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(1), System.ComponentModel.DataAnnotations.MaxLength(100)]
    public List<CreateOrderItemDto> Items { get; set; }
        = new();
    public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(
        System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(DeliveryPhone) && (DeliveryPhone.Count(char.IsDigit) < 7 || DeliveryPhone.Count(char.IsDigit) > 15))
            yield return new("Telefon nömrəsi 7–15 rəqəmdən ibarət olmalıdır.", new[] { nameof(DeliveryPhone) });
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
