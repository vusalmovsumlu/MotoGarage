using MotoGarage.Application.Helpers;
using MotoGarage.Domain.Exceptions;
using AutoMapper;
using MotoGarage.Application.DTOs.ServiceRecord;

using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Services;

public class ServiceRecordService : IServiceRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceRecordRepository _serviceRecordRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public ServiceRecordService(
        IServiceRecordRepository serviceRecordRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _serviceRecordRepository = serviceRecordRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ServiceRecordDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId)
    {
        var vehicle = await ValidateVehicleOwnershipAsync(vehicleId, userId);

        var serviceRecords =
            await _serviceRecordRepository.GetByVehicleIdAsync(vehicleId);

        return _mapper.Map<IEnumerable<ServiceRecordDto>>(serviceRecords);
    }

    public async Task<ServiceRecordDto> GetByIdAsync(
        int id,
        int userId)
    {
        var serviceRecord =
            await _serviceRecordRepository.GetByIdAsync(id);

        if (serviceRecord is null)
        {
            throw new KeyNotFoundException(
                "Servis qeydi tapılmadı.");
        }

        var vehicle = await ValidateVehicleOwnershipAsync(
            serviceRecord.VehicleId,
            userId);

        return _mapper.Map<ServiceRecordDto>(serviceRecord);
    }

    public async Task<ServiceRecordDto> CreateAsync(
        int userId,
        CreateServiceRecordDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var vehicle = await ValidateVehicleOwnershipAsync(
                dto.VehicleId,
                userId);

            var serviceRecord =
                _mapper.Map<ServiceRecord>(dto);

            await _serviceRecordRepository.AddAsync(serviceRecord);

            if (dto.Kilometer > vehicle.CurrentKm) vehicle.CurrentKm = dto.Kilometer;
            await _serviceRecordRepository.SaveChangesAsync();

            return _mapper.Map<ServiceRecordDto>(serviceRecord);
        });
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateServiceRecordDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var serviceRecord =
                await _serviceRecordRepository.GetByIdAsync(id);

            if (serviceRecord is null)
            {
                throw new KeyNotFoundException(
                    "Servis qeydi tapılmadı.");
            }

            var vehicle = await ValidateVehicleOwnershipAsync(
                serviceRecord.VehicleId,
                userId);

            _mapper.Map(dto, serviceRecord);

            await _serviceRecordRepository.UpdateAsync(serviceRecord);

            if (dto.Kilometer > vehicle.CurrentKm) vehicle.CurrentKm = dto.Kilometer;
            await _serviceRecordRepository.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var serviceRecord =
                await _serviceRecordRepository.GetByIdAsync(id);

            if (serviceRecord is null)
            {
                throw new KeyNotFoundException(
                    "Servis qeydi tapılmadı.");
            }

            var vehicle = await ValidateVehicleOwnershipAsync(
                serviceRecord.VehicleId,
                userId);

            await _serviceRecordRepository.DeleteAsync(serviceRecord);

            await _serviceRecordRepository.SaveChangesAsync();
        });
    }

    private async Task<Vehicle> ValidateVehicleOwnershipAsync(
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
            throw new ForbiddenException(
                "Bu nəqliyyat vasitəsinə giriş icazən yoxdur.");
        }
        return vehicle;
    }
}