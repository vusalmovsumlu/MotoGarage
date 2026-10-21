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
    private readonly IStockAlertService _stockAlerts;
    private readonly IAccountAccessService _accountAccess;
    private readonly IWorkflowEmailQueue _emails;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        INotificationService notifications,
        IAccountAccessService accountAccess, IStockAlertService stockAlerts, IWorkflowEmailQueue emails)
    {
        _notifications = notifications;
        _accountAccess = accountAccess;
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _mapper = mapper;
        _stockAlerts = stockAlerts;
        _emails = emails;
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
        return (await CreateOrdersAsync(userId, dto, allowMultipleStores: false)).Single();
    }

    public async Task<CheckoutDto> CheckoutAsync(int userId, CreateOrderDto dto)
    {
        var orders = await CreateOrdersAsync(userId, dto, allowMultipleStores: true);
        return new CheckoutDto { Orders = orders, TotalAmount = orders.Sum(x => x.TotalAmount) };
    }

    private Task<IReadOnlyList<OrderDto>> CreateOrdersAsync(
        int userId, CreateOrderDto dto, bool allowMultipleStores)
    {
        DtoValidation.Validate(dto);
        return _unitOfWork.ExecuteAsync<IReadOnlyList<OrderDto>>(async () =>
        {
            await _accountAccess.RequireParticipantAsync(userId);
            var productIds = dto.Items.Select(x => x.ProductId).ToList();
            if (productIds.Count != productIds.Distinct().Count())
                throw new DomainException("Eyni məhsulu sifarişə bir neçə dəfə əlavə etmək olmaz.");

            var products = await _productRepository.GetForOrderAsync(productIds);
            var productsById = products.ToDictionary(x => x.Id);
            // Validate every store/product before changing any stock. Product IDs
            // have a stable read/write order across concurrent checkout requests.
            foreach (var itemDto in dto.Items.OrderBy(x => x.ProductId))
            {
                if (!productsById.TryGetValue(itemDto.ProductId, out var product) || !product.IsActive)
                    throw new KeyNotFoundException($"Məhsul tapılmadı. Id: {itemDto.ProductId}");
                if (product.Store is null || !product.Store.IsActive)
                    throw new DomainException("Mağaza aktiv deyil.");
                if (product.Stock < itemDto.Quantity)
                    throw new DomainException($"{product.Name} üçün kifayət qədər stok yoxdur.");
            }

            var stores = products.Select(x => x.Store).DistinctBy(x => x.Id).ToDictionary(x => x.Id);
            if (!allowMultipleStores && stores.Count > 1)
                throw new DomainException("Bir sifarişdə yalnız eyni mağazanın məhsulları ola bilər.");
            foreach (var store in stores.Values.OrderBy(x => x.Id))
                await _accountAccess.RequireParticipantAsync(store.UserId);

            foreach (var itemDto in dto.Items.OrderBy(x => x.ProductId))
            {
                var product = productsById[itemDto.ProductId];
                product.Stock -= itemDto.Quantity;
                await _stockAlerts.EvaluateAsync(product);
                await _productRepository.UpdateAsync(product);
            }

            var orders = new List<Order>();
            var createdAt = DateTime.UtcNow;
            foreach (var group in dto.Items.GroupBy(x => productsById[x.ProductId].StoreId).OrderBy(x => x.Key))
            {
                var items = group.OrderBy(x => x.ProductId).Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Product = productsById[item.ProductId],
                    Quantity = item.Quantity,
                    UnitPrice = productsById[item.ProductId].Price,
                    TotalPrice = productsById[item.ProductId].Price * item.Quantity
                }).ToList();
                var order = new Order
                {
                    UserId = userId,
                    StoreId = group.Key,
                    Store = stores[group.Key],
                    RecipientName = dto.RecipientName.Trim(),
                    DeliveryPhone = dto.DeliveryPhone.Trim(),
                    DeliveryCity = dto.DeliveryCity.Trim(),
                    DeliveryAddress = dto.DeliveryAddress.Trim(),
                    DeliveryNote = string.IsNullOrWhiteSpace(dto.DeliveryNote) ? null : dto.DeliveryNote.Trim(),
                    TotalAmount = items.Sum(x => x.TotalPrice),
                    Status = OrderStatus.Pending,
                    CreatedAt = createdAt,
                    OrderItems = items
                };
                await _orderRepository.AddAsync(order);
                orders.Add(order);
            }

            // All store orders, stock, email snapshots and notifications share
            // one UnitOfWork transaction; any failure rolls back the whole cart.
            await _orderRepository.SaveChangesAsync();
            foreach (var order in orders)
            {
                await _emails.OrderAsync(order.Id);
                await _notifications.CreateAsync(stores[order.StoreId].UserId, NotificationType.Order,
                    "Yeni sifariş", $"Yeni sifariş yaradıldı: #{order.Id}.", $"/orders/{order.Id}");
            }
            return _mapper.Map<List<OrderDto>>(orders);
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
            if (dto.Status == OrderStatus.Completed) order.CompletedAtUtc = DateTime.UtcNow;
            if (dto.Status == OrderStatus.Cancelled) order.CancelledAtUtc = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            await _orderRepository.SaveChangesAsync();
            await _emails.OrderAsync(order.Id);
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
            order.CancelledAtUtc = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);

            await _orderRepository.SaveChangesAsync();
            await _emails.OrderAsync(order.Id);
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
            await _stockAlerts.EvaluateAsync(product);
        }
    }
}
