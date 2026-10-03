using AutoMapper;
using MotoGarage.Application.DTOs.Mechanic;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class MechanicService : IMechanicService
{
    private readonly IMechanicRepository _mechanicRepository;
    private readonly IMapper _mapper;

    public MechanicService(
        IMechanicRepository mechanicRepository,
        IMapper mapper)
    {
        _mechanicRepository = mechanicRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MechanicDto>> GetAllAsync()
    {
        var mechanics =
            await _mechanicRepository.GetActiveMechanicsAsync();

        return _mapper.Map<IEnumerable<MechanicDto>>(mechanics);
    }

    public async Task<MechanicDto> GetByIdAsync(int id)
    {
        var mechanic =
            await _mechanicRepository.GetByIdAsync(id);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta tapılmadı.");
        }

        return _mapper.Map<MechanicDto>(mechanic);
    }

    public async Task<MechanicDto> GetMyProfileAsync(int userId)
    {
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta profili tapılmadı.");
        }

        return _mapper.Map<MechanicDto>(mechanic);
    }

    public async Task<IEnumerable<MechanicDto>> SearchAsync(
        string? search,
        string? address)
    {
        var mechanics =
            await _mechanicRepository.SearchAsync(
                search,
                address);

        return _mapper.Map<IEnumerable<MechanicDto>>(mechanics);
    }

    public async Task<MechanicDto> CreateAsync(
        int userId,
        CreateMechanicDto dto)
    {
        var existingMechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (existingMechanic is not null)
        {
            throw new DomainException(
                "Bu istifadəçi üçün artıq usta profili mövcuddur.");
        }

        var mechanic =
            _mapper.Map<Mechanic>(dto);

        mechanic.UserId = userId;
        mechanic.Rating = 0;
        mechanic.IsActive = true;
        mechanic.CreatedAt = DateTime.UtcNow;

        await _mechanicRepository.AddAsync(mechanic);

        await _mechanicRepository.SaveChangesAsync();

        return _mapper.Map<MechanicDto>(mechanic);
    }

    public async Task UpdateAsync(
        int userId,
        UpdateMechanicDto dto)
    {
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta profili tapılmadı.");
        }

        _mapper.Map(dto, mechanic);

        await _mechanicRepository.UpdateAsync(mechanic);

        await _mechanicRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int userId)
    {
        var mechanic =
            await _mechanicRepository.GetByUserIdAsync(userId);

        if (mechanic is null)
        {
            throw new KeyNotFoundException(
                "Usta profili tapılmadı.");
        }

        mechanic.IsActive = false;

        await _mechanicRepository.UpdateAsync(mechanic);

        await _mechanicRepository.SaveChangesAsync();
    }
}