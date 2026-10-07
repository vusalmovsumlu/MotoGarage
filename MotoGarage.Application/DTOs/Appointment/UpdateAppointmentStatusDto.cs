using System.ComponentModel.DataAnnotations;
using MotoGarage.Domain.Enums;

namespace MotoGarage.Application.DTOs.Appointment;

public class UpdateAppointmentStatusDto
{
    [EnumDataType(typeof(AppointmentStatus))]
    public AppointmentStatus Status { get; set; }
}