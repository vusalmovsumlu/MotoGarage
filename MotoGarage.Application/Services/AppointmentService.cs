using MotoGarage.Application.Helpers;
using AutoMapper;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;
    private readonly IAccountAccessService _accountAccess;
    private readonly IAppointmentPlanningService _planning;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMechanicRepository mechanicRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        IAccountAccessService accountAccess, IAppointmentPlanningService planning)
    {
        _notifications = notifications;
        _accountAccess = accountAccess;
        _planning = planning;
        _unitOfWork = unitOfWork;
        _appointmentRepository = appointmentRepository;
        _mechanicRepository = mechanicRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    // =========================
    // User appointments
    // =========================

    public async Task<IEnumerable<AppointmentDto>> GetMyAppointmentsAsync(
        int userId)
    {
        var appointments =
            await _appointmentRepository.GetByUserIdAsync(userId);

        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    // =========================
    // Mechanic appointments
    // =========================

    public async Task<IEnumerable<AppointmentDto>>
        GetMechanicAppointmentsAsync(int mechanicUserId)
    {
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(mechanicUserId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta profili tapılmadı.");
        }

        var appointments =
            await _appointmentRepository
                .GetByMechanicIdAsync(mechanic.Id);

        return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
    }

    // =========================
    // Get by id
    // =========================

    public async Task<AppointmentDto> GetByIdAsync(
        int id,
        int currentUserId)
    {
        var appointment =
            await _appointmentRepository.GetDetailsByIdAsync(id);

        if (appointment is null)
        {
            throw new KeyNotFoundException(
                "Randevu tapılmadı.");
        }

        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(currentUserId);

        var isOwner =
            appointment.UserId == currentUserId;

        var isMechanic =
            mechanic is not null &&
            appointment.MechanicId == mechanic.Id;

        if (!isOwner && !isMechanic)
        {
            throw new ForbiddenException(
                "Bu randevuya baxmaq icazən yoxdur.");
        }

        return _mapper.Map<AppointmentDto>(appointment);
    }

    // =========================
    // Create
    // =========================

    public async Task<AppointmentDto> CreateAsync(
        int userId,
        CreateAppointmentDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            await _accountAccess.RequireParticipantAsync(userId);
            var vehicle =
                await _vehicleRepository.GetByIdAsync(dto.VehicleId);

            if (vehicle is null)
            {
                throw new KeyNotFoundException(
                    "Nəqliyyat vasitəsi tapılmadı.");
            }

            if (vehicle.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu nəqliyyat vasitəsi sənə aid deyil.");
            }

            var mechanic =
                await _mechanicRepository.GetByIdAsync(dto.MechanicId);

            if (mechanic is null || !mechanic.IsActive)
            {
                throw new KeyNotFoundException(
                    "Usta tapılmadı.");
            }

            dto.AppointmentDate = dto.AppointmentDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dto.AppointmentDate, DateTimeKind.Utc)
                : dto.AppointmentDate.ToUniversalTime();
            await _planning.ValidateSlotAsync(dto.MechanicId, dto.VehicleId, dto.AppointmentDate, dto.DurationMinutes);
            if (await _appointmentRepository.HasActiveSlotAsync(dto.MechanicId, dto.AppointmentDate))
                throw new ConflictException("Bu vaxt üçün aktiv randevu artıq mövcuddur.");
            if (dto.AppointmentDate <= DateTime.UtcNow)
            {
                throw new DomainException(
                    "Randevu tarixi keçmiş tarix ola bilməz.");
            }

            var appointment =
                _mapper.Map<Appointment>(dto);

            appointment.UserId = userId;
            appointment.Status = AppointmentStatus.Pending;
            appointment.CreatedAt = DateTime.UtcNow;

            await _appointmentRepository.AddAsync(appointment);

            await _appointmentRepository.SaveChangesAsync();

            await _notifications.CreateAsync(mechanic.UserId, NotificationType.Appointment,
                "Yeni randevu", $"Yeni randevu yaradıldı: #{appointment.Id}.", $"/appointments/{appointment.Id}");
            return _mapper.Map<AppointmentDto>(await _appointmentRepository.GetDetailsByIdAsync(appointment.Id));
        });
    }

    // =========================
    // Mechanic changes status
    // =========================

    public async Task UpdateStatusAsync(
        int appointmentId,
        int mechanicUserId,
        UpdateAppointmentStatusDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var mechanic =
                await _mechanicRepository.GetByUserIdAsync(mechanicUserId);

            if (mechanic is null)
            {
                throw new KeyNotFoundException(
                    "Usta profili tapılmadı.");
            }

            var appointment =
                await _appointmentRepository.GetByIdAsync(appointmentId);

            if (appointment is null)
            {
                throw new KeyNotFoundException(
                    "Randevu tapılmadı.");
            }

            if (appointment.MechanicId != mechanic.Id)
            {
                throw new ForbiddenException(
                    "Bu randevu sənə aid deyil.");
            }

            if (dto.Status == AppointmentStatus.Cancelled)
                throw new DomainException("Usta yalnız qəbul, rədd və tamamlanma statusu seçə bilər.");
            StatusTransitions.Validate(appointment.Status, dto.Status);

            appointment.Status = dto.Status;
            if (dto.Status == AppointmentStatus.Completed) appointment.CompletedAtUtc = DateTime.UtcNow;
            if (dto.Status is AppointmentStatus.Completed or AppointmentStatus.Rejected)
            {
                appointment.ProposedAppointmentDateUtc = null;
                appointment.RescheduleProposedByUserId = null;
            }

            await _appointmentRepository.UpdateAsync(appointment);

            await _appointmentRepository.SaveChangesAsync();
            await _notifications.CreateAsync(appointment.UserId, NotificationType.Appointment,
                "Randevu statusu", $"Randevu #{appointment.Id}: {appointment.Status}.", $"/appointments/{appointment.Id}");
        });
    }

    // =========================
    // User cancellation
    // =========================

    public async Task CancelAsync(
        int appointmentId,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var appointment =
                await _appointmentRepository.GetByIdAsync(appointmentId);

            if (appointment is null)
            {
                throw new KeyNotFoundException(
                    "Randevu tapılmadı.");
            }

            if (appointment.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu randevu sənə aid deyil.");
            }

            if (appointment.Status == AppointmentStatus.Cancelled) return;
            StatusTransitions.Validate(appointment.Status, AppointmentStatus.Cancelled);

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.ProposedAppointmentDateUtc = null;
            appointment.RescheduleProposedByUserId = null;

            await _appointmentRepository.UpdateAsync(appointment);

            await _appointmentRepository.SaveChangesAsync();
            await _notifications.CreateAsync(appointment.UserId, NotificationType.Appointment,
                "Randevu statusu", $"Randevu #{appointment.Id}: {appointment.Status}.", $"/appointments/{appointment.Id}");
        });
    }
}
