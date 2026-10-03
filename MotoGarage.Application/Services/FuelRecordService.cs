using AutoMapper;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Services;

public class FuelRecordService : IFuelRecordService
{
    private readonly IFuelRecordRepository _fuelRecordRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public FuelRecordService(
        IFuelRecordRepository fuelRecordRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _fuelRecordRepository = fuelRecordRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<FuelRecordDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        var records =
            await _fuelRecordRepository
                .GetByVehicleIdAsync(vehicleId);

        return _mapper.Map<IEnumerable<FuelRecordDto>>(records);
    }

    public async Task<FuelRecordDto> GetByIdAsync(
        int id,
        int userId)
    {
        var record =
            await _fuelRecordRepository.GetByIdAsync(id);

        if (record is null)
        {
            throw new KeyNotFoundException(
                "Yanacaq qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            record.VehicleId,
            userId);

        return _mapper.Map<FuelRecordDto>(record);
    }

    public async Task<FuelRecordDto> CreateAsync(
        int userId,
        CreateFuelRecordDto dto)
    {
        await ValidateVehicleOwnershipAsync(
            dto.VehicleId,
            userId);

        var record =
            _mapper.Map<FuelRecord>(dto);

        await _fuelRecordRepository.AddAsync(record);

        await _fuelRecordRepository.SaveChangesAsync();

        return _mapper.Map<FuelRecordDto>(record);
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateFuelRecordDto dto)
    {
        var record =
            await _fuelRecordRepository.GetByIdAsync(id);

        if (record is null)
        {
            throw new KeyNotFoundException(
                "Yanacaq qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            record.VehicleId,
            userId);

        _mapper.Map(dto, record);

        await _fuelRecordRepository.UpdateAsync(record);

        await _fuelRecordRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        var record =
            await _fuelRecordRepository.GetByIdAsync(id);

        if (record is null)
        {
            throw new KeyNotFoundException(
                "Yanacaq qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            record.VehicleId,
            userId);

        await _fuelRecordRepository.DeleteAsync(record);

        await _fuelRecordRepository.SaveChangesAsync();
    }

    private async Task ValidateVehicleOwnershipAsync(
        int vehicleId,
        int userId)
    {
        var vehicle =
            await _vehicleRepository.GetByIdAsync(vehicleId);

        if (vehicle is null)
        {
            throw new KeyNotFoundException(
                "Nəqliyyat vasitəsi tapılmadı.");
        }

        if (vehicle.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "Bu nəqliyyat vasitəsinə giriş icazən yoxdur.");
        }
    }
}