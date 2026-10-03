using AutoMapper;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IMechanicRepository mechanicRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
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
            throw new UnauthorizedAccessException(
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
        var vehicle =
            await _vehicleRepository.GetByIdAsync(dto.VehicleId);

        if (vehicle is null)
        {
            throw new KeyNotFoundException(
                "Nəqliyyat vasitəsi tapılmadı.");
        }

        if (vehicle.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "Bu nəqliyyat vasitəsi sənə aid deyil.");
        }

        var mechanic =
            await _mechanicRepository.GetByIdAsync(dto.MechanicId);

        if (mechanic is null || !mechanic.IsActive)
        {
            throw new KeyNotFoundException(
                "Usta tapılmadı.");
        }

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

        return _mapper.Map<AppointmentDto>(appointment);
    }

    // =========================
    // Mechanic changes status
    // =========================

    public async Task UpdateStatusAsync(
        int appointmentId,
        int mechanicUserId,
        UpdateAppointmentStatusDto dto)
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
            throw new UnauthorizedAccessException(
                "Bu randevu sənə aid deyil.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled ||
            appointment.Status == AppointmentStatus.Completed)
        {
            throw new DomainException(
                "Bu randevunun statusu artıq dəyişdirilə bilməz.");
        }

        if (dto.Status != AppointmentStatus.Accepted &&
            dto.Status != AppointmentStatus.Rejected &&
            dto.Status != AppointmentStatus.Completed)
        {
            throw new DomainException(
                "Usta üçün uyğun olmayan status seçilib.");
        }

        appointment.Status = dto.Status;

        await _appointmentRepository.UpdateAsync(appointment);

        await _appointmentRepository.SaveChangesAsync();
    }

    // =========================
    // User cancellation
    // =========================

    public async Task CancelAsync(
        int appointmentId,
        int userId)
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
            throw new UnauthorizedAccessException(
                "Bu randevu sənə aid deyil.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            throw new DomainException(
                "Tamamlanmış randevu ləğv edilə bilməz.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new DomainException(
                "Randevu artıq ləğv edilib.");
        }

        appointment.Status = AppointmentStatus.Cancelled;

        await _appointmentRepository.UpdateAsync(appointment);

        await _appointmentRepository.SaveChangesAsync();
    }
}