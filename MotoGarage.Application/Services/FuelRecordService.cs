using MotoGarage.Application.Helpers;
using AutoMapper;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class FuelRecordService : IFuelRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFuelRecordRepository _fuelRecordRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public FuelRecordService(
        IFuelRecordRepository fuelRecordRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _fuelRecordRepository = fuelRecordRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<FuelRecordDto>>
        GetByVehicleIdAsync(
            int vehicleId,
            int userId)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        var records =
            await _fuelRecordRepository
                .GetByVehicleIdAsync(vehicleId);

        return _mapper.Map<
            IEnumerable<FuelRecordDto>>(records);
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

    // =========================
    // Fuel Consumption Stats
    // =========================

    public async Task<FuelConsumptionStatsDto>
        GetStatsAsync(
            int vehicleId,
            int userId,
            DateTime? startDate,
            DateTime? endDate)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        if (startDate.HasValue != endDate.HasValue)
        {
            throw new DomainException(
                "Başlanğıc və bitiş tarixləri birlikdə göndərilməlidir.");
        }

        if (startDate.HasValue &&
            endDate.HasValue &&
            startDate.Value.Date > endDate.Value.Date)
        {
            throw new DomainException(
                "Başlanğıc tarixi bitiş tarixindən böyük ola bilməz.");
        }

        List<FuelRecord> records;
        if (startDate.HasValue && endDate.HasValue)
        {
            var range = DateRange.Days(startDate.Value, endDate.Value);
            records = (await _fuelRecordRepository.GetByDateRangeAsync(
                vehicleId, range.Start, range.EndExclusive)).ToList();
        }
        else records = (await _fuelRecordRepository.GetByVehicleIdAsync(vehicleId)).ToList();

        records = records
            .OrderBy(x => x.Kilometer)
            .ThenBy(x => x.FuelDate)
            .ToList();

        if (records.Count == 0)
        {
            return new FuelConsumptionStatsDto
            {
                VehicleId = vehicleId,
                RecordCount = 0,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        var firstRecord = records.First();
        var lastRecord = records.Last();

        var distanceKm =
            lastRecord.Kilometer -
            firstRecord.Kilometer;

        if (distanceKm < 0)
        {
            distanceKm = 0;
        }

        var totalLiters =
    records.Sum(x => x.Liters);

        var totalFuelCost =
            records.Sum(x => x.TotalPrice);

        // İlk qeyd başlanğıc nöqtəsidir.
        // Sərfiyyat üçün sonrakı doldurmaları hesablayırıq.
        var consumedLiters =
            records
                .Skip(1)
                .Sum(x => x.Liters);

        decimal averageConsumption = 0;

        if (distanceKm > 0 &&
            records.Count >= 2)
        {
            averageConsumption =
                consumedLiters /
                distanceKm *
                100;

            averageConsumption =
                Math.Round(
                    averageConsumption,
                    2);
        }

        return new FuelConsumptionStatsDto
        {
            VehicleId = vehicleId,

            RecordCount = records.Count,

            StartKm = firstRecord.Kilometer,

            EndKm = lastRecord.Kilometer,

            DistanceKm = distanceKm,

            TotalLiters =
                Math.Round(totalLiters, 2),

            TotalFuelCost =
                Math.Round(totalFuelCost, 2),

            AverageConsumptionPer100Km =
                averageConsumption,

            StartDate = startDate,

            EndDate = endDate
        };
    }

    public async Task<FuelRecordDto> CreateAsync(
        int userId,
        CreateFuelRecordDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var vehicle =
                await ValidateVehicleOwnershipAsync(
                    dto.VehicleId,
                    userId);

            if (dto.Liters <= 0)
            {
                throw new DomainException(
                    "Yanacaq miqdarı 0-dan böyük olmalıdır.");
            }

            if (dto.PricePerLiter <= 0)
            {
                throw new DomainException(
                    "Litr qiyməti 0-dan böyük olmalıdır.");
            }

            if (dto.Kilometer < 0)
            {
                throw new DomainException(
                    "Kilometr mənfi ola bilməz.");
            }

            var record =
                _mapper.Map<FuelRecord>(dto);

            // AutoMapper TotalPrice-i hesablayır
            await _fuelRecordRepository.AddAsync(record);

            // Yeni km maşının cari km-dən böyükdürsə yenilə
            if (dto.Kilometer > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Kilometer;

                await _vehicleRepository.UpdateAsync(vehicle);
            }

            await _fuelRecordRepository.SaveChangesAsync();

            return _mapper.Map<FuelRecordDto>(record);
        });
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateFuelRecordDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var record =
                await _fuelRecordRepository.GetByIdAsync(id);

            if (record is null)
            {
                throw new KeyNotFoundException(
                    "Yanacaq qeydi tapılmadı.");
            }

            var vehicle =
                await ValidateVehicleOwnershipAsync(
                    record.VehicleId,
                    userId);

            if (dto.Liters <= 0)
            {
                throw new DomainException(
                    "Yanacaq miqdarı 0-dan böyük olmalıdır.");
            }

            if (dto.PricePerLiter <= 0)
            {
                throw new DomainException(
                    "Litr qiyməti 0-dan böyük olmalıdır.");
            }

            if (dto.Kilometer < 0)
            {
                throw new DomainException(
                    "Kilometr mənfi ola bilməz.");
            }

            _mapper.Map(dto, record);

            await _fuelRecordRepository.UpdateAsync(record);

            if (dto.Kilometer > vehicle.CurrentKm)
            {
                vehicle.CurrentKm = dto.Kilometer;

                await _vehicleRepository.UpdateAsync(vehicle);
            }

            await _fuelRecordRepository.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
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
        });
    }

    private async Task<Vehicle>
        ValidateVehicleOwnershipAsync(
            int vehicleId,
            int userId)
    {
        var vehicle =
            await _vehicleRepository.GetByIdAsync(
                vehicleId);

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
