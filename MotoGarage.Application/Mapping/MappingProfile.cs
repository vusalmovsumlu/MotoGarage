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
            .ForMember(dest => dest.NotificationSentAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.EmailQueuedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduleRevision, opt => opt.Ignore());
        CreateMap<UpdateReminderDto, Reminder>()
            .ForMember(dest => dest.NotificationSentAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.EmailQueuedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduleRevision, opt => opt.Ignore());


        // =========================
        // Mechanic
        // =========================

        CreateMap<Mechanic, MechanicDto>()
            .ForMember(x => x.FullName, o => o.MapFrom(x => x.User != null ? x.User.FullName : ""));
        CreateMap<CreateMechanicDto, Mechanic>();
        CreateMap<UpdateMechanicDto, Mechanic>();


        // =========================
        // Appointment
        // =========================

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.VehicleName, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.Brand + " " + src.Vehicle.Model : null))
            .ForMember(dest => dest.MechanicName, opt => opt.MapFrom(src => src.Mechanic != null
                ? src.Mechanic.User != null && !string.IsNullOrWhiteSpace(src.Mechanic.User.FullName)
                    ? src.Mechanic.User.FullName : src.Mechanic.BusinessName
                : null))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null));
        CreateMap<CreateAppointmentDto, Appointment>();


        // =========================
        // Review
        // =========================

        CreateMap<Review, ReviewDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User.FullName));
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

        CreateMap<Product, ProductDto>()
            .ForMember(x => x.StoreName, o => o.MapFrom(x => x.Store != null ? x.Store.StoreName : null));
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.LowStockThreshold, opt => opt.Condition(src => src.LowStockThreshold.HasValue));


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
            .ForMember(x => x.StoreName, o => o.MapFrom(x => x.Store != null ? x.Store.StoreName : null))
            .ForMember(
                dest => dest.Items,
                opt => opt.MapFrom(
                    src => src.OrderItems));


        // =========================
        // Chat
        // =========================

        CreateMap<Conversation, ConversationDto>()
            .ForMember(x => x.UserFullName, o => o.MapFrom(x => x.User.FullName))
            .ForMember(x => x.MechanicName, o => o.MapFrom(x => x.Mechanic != null ? x.Mechanic.BusinessName : null))
            .ForMember(x => x.StoreName, o => o.MapFrom(x => x.Store != null ? x.Store.StoreName : null))
            .ForMember(x => x.ParticipantName, o => o.Ignore())
            .ForMember(x => x.Slug, o => o.Ignore());

        CreateMap<Message, MessageDto>();
        CreateMap<ChatAttachment, ChatAttachmentDto>();


        // =========================
        // Notification
        // =========================

        CreateMap<Notification, NotificationDto>();
    }
}
