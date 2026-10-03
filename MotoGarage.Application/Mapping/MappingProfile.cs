using AutoMapper;
using MotoGarage.Application.DTOs.Expense;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.DTOs.ServiceRecord;
using MotoGarage.Application.DTOs.Vehicle;
using MotoGarage.Domain.Entities;

namespace MotoGarage.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // =========================
        // Vehicle
        // =========================

        CreateMap<Vehicle, VehicleDto>();

        CreateMap<CreateVehicleDto, Vehicle>();

        CreateMap<UpdateVehicleDto, Vehicle>();


        // =========================
        // Service Record
        // =========================

        CreateMap<ServiceRecord, ServiceRecordDto>();

        CreateMap<CreateServiceRecordDto, ServiceRecord>();

        CreateMap<UpdateServiceRecordDto, ServiceRecord>();


        // =========================
        // Fuel Record
        // =========================

        CreateMap<FuelRecord, FuelRecordDto>();

        CreateMap<CreateFuelRecordDto, FuelRecord>()
            .ForMember(
                dest => dest.TotalPrice,
                opt => opt.MapFrom(src =>
                    src.Liters * src.PricePerLiter));

        CreateMap<UpdateFuelRecordDto, FuelRecord>()
            .ForMember(
                dest => dest.TotalPrice,
                opt => opt.MapFrom(src =>
                    src.Liters * src.PricePerLiter));


        // =========================
        // Expense
        // =========================

        CreateMap<Expense, ExpenseDto>();

        CreateMap<CreateExpenseDto, Expense>();

        CreateMap<UpdateExpenseDto, Expense>();


        // =========================
        // Reminder
        // =========================

        CreateMap<Reminder, ReminderDto>();

        CreateMap<CreateReminderDto, Reminder>();

        CreateMap<UpdateReminderDto, Reminder>();
    }
}