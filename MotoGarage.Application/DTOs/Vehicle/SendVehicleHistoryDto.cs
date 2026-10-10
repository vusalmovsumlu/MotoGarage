using System.ComponentModel.DataAnnotations;

namespace MotoGarage.Application.DTOs.Vehicle;

public class SendVehicleHistoryDto
{
    [Required, EmailAddress, StringLength(254)]
    public string RecipientEmail { get; set; } = null!;
    public bool ConfirmSharing { get; set; }
}

public record VehicleHistoryFile(byte[] Content, string FileName, string VehicleName)
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
