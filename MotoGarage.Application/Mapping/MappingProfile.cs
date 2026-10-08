using AutoMapper;
using MotoGarage.Application.DTOs.Appointment;
using MotoGarage.Application.DTOs.Chat;
using MotoGarage.Application.DTOs.Expense;
using MotoGarage.Application.DTOs.FuelRecord;
using MotoGarage.Application.DTOs.Mechanic;
using MotoGarage.Application.DTOs.Notification;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.DTOs.Product;
using MotoGarage.Application.DTOs.Reminder;
using MotoGarage.Application.DTOs.Review;
using MotoGarage.Application.DTOs.ServiceRecord;
using MotoGarage.Application.DTOs.Store;
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
        CreateMap<CreateVehicleDto, Vehicle>()
            .ForMember(x => x.Slug, opt => opt.Ignore());
        CreateMap<UpdateVehicleDto, Vehicle>()
            .ForMember(x => x.Slug, opt => opt.Ignore());


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
                opt => opt.MapFrom(
                    src =>
                        src.Liters *
                        src.PricePerLiter));

        CreateMap<UpdateFuelRecordDto, FuelRecord>()
            .ForMember(
                dest => dest.TotalPrice,
                opt => opt.MapFrom(
                    src =>
                        src.Liters *
                        src.PricePerLiter));


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
        CreateMap<CreateReminderDto, Reminder>()
            .ForMember(dest => dest.NotificationSentAtUtc, opt => opt.Ignore());
        CreateMap<UpdateReminderDto, Reminder>()
            .ForMember(dest => dest.NotificationSentAtUtc, opt => opt.Ignore());


        // =========================
        // Mechanic
        // =========================

        CreateMap<Mechanic, MechanicDto>();
        CreateMap<CreateMechanicDto, Mechanic>();
        CreateMap<UpdateMechanicDto, Mechanic>();


        // =========================
        // Appointment
        // =========================

        CreateMap<Appointment, AppointmentDto>();
        CreateMap<CreateAppointmentDto, Appointment>();


        // =========================
        // Review
        // =========================

        CreateMap<Review, ReviewDto>();
        CreateMap<CreateReviewDto, Review>();
        CreateMap<UpdateReviewDto, Review>();


        // =========================
        // Store
        // =========================

        CreateMap<Store, StoreDto>();
        CreateMap<CreateStoreDto, Store>();
        CreateMap<UpdateStoreDto, Store>();


        // =========================
        // Product
        // =========================

        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();


        // =========================
        // Order
        // =========================

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(
                dest => dest.ProductName,
                opt => opt.MapFrom(
                    src =>
                        src.Product != null
                            ? src.Product.Name
                            : string.Empty));

        CreateMap<Order, OrderDto>()
            .ForMember(
                dest => dest.Items,
                opt => opt.MapFrom(
                    src => src.OrderItems));


        // =========================
        // Chat
        // =========================

        CreateMap<Conversation, ConversationDto>();

        CreateMap<Message, MessageDto>();


        // =========================
        // Notification
        // =========================

        CreateMap<Notification, NotificationDto>();
    }
}
