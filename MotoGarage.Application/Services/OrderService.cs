using MotoGarage.Application.Helpers;
using AutoMapper;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.Interfaces.Repositories;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Enums;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class OrderService : IOrderService
{
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        INotificationService notifications)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _mapper = mapper;
    }

    // =========================
    // User orders
    // =========================

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync(
        int userId)
    {
        var orders =
            await _orderRepository.GetByUserIdAsync(userId);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    // =========================
    // Store orders
    // =========================

    public async Task<IEnumerable<OrderDto>> GetStoreOrdersAsync(
        int storeUserId)
    {
        var store =
            await GetStoreByUserIdAsync(storeUserId);

        var orders =
            await _orderRepository.GetByStoreIdAsync(store.Id);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    // =========================
    // Order details
    // =========================

    public async Task<OrderDto> GetByIdAsync(
        int id,
        int currentUserId)
    {
        var order =
            await _orderRepository.GetDetailsByIdAsync(id);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Sifariş tapılmadı.");
        }

        var isCustomer =
            order.UserId == currentUserId;

        var store =
            await _storeRepository.GetByUserIdAsync(
                currentUserId);

        var isStoreOwner =
            store is not null &&
            order.StoreId == store.Id;

        if (!isCustomer && !isStoreOwner)
        {
            throw new ForbiddenException(
                "Bu sifarişə baxmaq icazən yoxdur.");
        }

        return _mapper.Map<OrderDto>(order);
    }

    // =========================
    // Create order
    // =========================

    public async Task<OrderDto> CreateAsync(
        int userId,
        CreateOrderDto dto)
    {
        DtoValidation.Validate(dto);
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            DtoValidation.Validate(dto);
            if (dto.Items is null || dto.Items.Count == 0)
            {
                throw new DomainException(
                    "Sifarişdə ən azı bir məhsul olmalıdır.");
            }

            var productIds =
                dto.Items
                    .Select(x => x.ProductId)
                    .ToList();

            if (productIds.Count != productIds.Distinct().Count())
            {
                throw new DomainException(
                    "Eyni məhsulu sifarişə bir neçə dəfə əlavə etmək olmaz.");
            }

            var orderItems = new List<OrderItem>();

            int? storeId = null;

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items.OrderBy(x => x.ProductId))
            {
                if (itemDto.Quantity <= 0)
                {
                    throw new DomainException(
                        "Məhsul sayı 0-dan böyük olmalıdır.");
                }

                var product =
                    await _productRepository
                        .GetByIdAsync(itemDto.ProductId);

                if (product is null || !product.IsActive)
                {
                    throw new KeyNotFoundException(
                        $"Məhsul tapılmadı. Id: {itemDto.ProductId}");
                }

                // Bir sifariş yalnız bir mağazaya aid olsun
                if (storeId is null)
                {
                    storeId = product.StoreId;
                }
                else if (storeId.Value != product.StoreId)
                {
                    throw new DomainException(
                        "Bir sifarişdə yalnız eyni mağazanın məhsulları ola bilər.");
                }

                if (product.Stock < itemDto.Quantity)
                {
                    throw new DomainException(
                        $"{product.Name} üçün kifayət qədər stok yoxdur.");
                }

                var itemTotal =
                    product.Price * itemDto.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = itemTotal
                });

                totalAmount += itemTotal;

                // stok azaldılır
                product.Stock -= itemDto.Quantity;

                await _productRepository.UpdateAsync(product);
            }

            if (storeId is null)
            {
                throw new DomainException(
                    "Sifariş üçün mağaza müəyyən edilə bilmədi.");
            }

            var store =
                await _storeRepository.GetByIdAsync(storeId.Value);

            if (store is null || !store.IsActive)
            {
                throw new DomainException(
                    "Mağaza aktiv deyil.");
            }

            var order = new Order
            {
                UserId = userId,
                StoreId = storeId.Value,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            await _orderRepository.AddAsync(order);

            // Eyni DbContext olduğu üçün order və stock dəyişiklikləri
            // bir SaveChanges ilə DB-yə yazılır.
            await _orderRepository.SaveChangesAsync();

            await _notifications.CreateAsync(store.UserId, NotificationType.Order,
                "Yeni sifariş", $"Yeni sifariş yaradıldı: #{order.Id}.", $"/orders/{order.Id}");
            return _mapper.Map<OrderDto>(order);
        });
    }

    // =========================
    // Store changes status
    // =========================

    public async Task UpdateStatusAsync(
        int orderId,
        int storeUserId,
        UpdateOrderStatusDto dto)
    {
        DtoValidation.Validate(dto);
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var store =
                await GetStoreByUserIdAsync(storeUserId);

            var order =
                await _orderRepository.GetDetailsByIdAsync(orderId);

            if (order is null)
            {
                throw new KeyNotFoundException(
                    "Sifariş tapılmadı.");
            }

            if (order.StoreId != store.Id)
            {
                throw new ForbiddenException(
                    "Bu sifariş sənin mağazana aid deyil.");
            }

            ValidateStatusTransition(
                order.Status,
                dto.Status);

            if (dto.Status == OrderStatus.Cancelled)
                await RestoreStockAsync(order);
            order.Status = dto.Status;

            await _orderRepository.UpdateAsync(order);

            await _orderRepository.SaveChangesAsync();
            await _notifications.CreateAsync(order.UserId, NotificationType.Order,
                "Sifariş statusu", $"Sifariş #{order.Id}: {order.Status}.", $"/orders/{order.Id}");
        });
    }

    // =========================
    // User cancels order
    // =========================

    public async Task CancelAsync(
        int orderId,
        int userId)
    {
        await _unitOfWork.ExecuteAsync(async () =>
        {
            var order =
                await _orderRepository.GetDetailsByIdAsync(orderId);

            if (order is null)
            {
                throw new KeyNotFoundException(
                    "Sifariş tapılmadı.");
            }

            if (order.UserId != userId)
            {
                throw new ForbiddenException(
                    "Bu sifariş sənə aid deyil.");
            }

            if (order.Status == OrderStatus.Cancelled) return;
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                throw new DomainException("Bu mərhələdə sifarişi ləğv etmək olmaz.");
            ValidateStatusTransition(order.Status, OrderStatus.Cancelled);
            await RestoreStockAsync(order);

            order.Status = OrderStatus.Cancelled;

            await _orderRepository.UpdateAsync(order);

            await _orderRepository.SaveChangesAsync();
            await _notifications.CreateAsync(order.UserId, NotificationType.Order,
                "Sifariş statusu", $"Sifariş #{order.Id}: {order.Status}.", $"/orders/{order.Id}");
        });
    }

    // =========================
    // Helpers
    // =========================

    private async Task<Store> GetStoreByUserIdAsync(
        int userId)
    {
        var store =
            await _storeRepository.GetByUserIdAsync(userId);

        if (store is null || !store.IsActive)
        {
            throw new ForbiddenException(
                "Aktiv mağaza profilin yoxdur.");
        }

        return store;
    }

    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        StatusTransitions.Validate(currentStatus, newStatus);
    }

    private async Task RestoreStockAsync(Order order)
    {
        foreach (var item in order.OrderItems.OrderBy(x => x.ProductId))
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId)
                ?? throw new ConflictException("Sifariş məhsulu tapılmadı.");
            if (product.Stock > int.MaxValue - item.Quantity)
                throw new ConflictException("Stokun maksimum həddi keçilir.");
            product.Stock += item.Quantity;
        }
    }
}