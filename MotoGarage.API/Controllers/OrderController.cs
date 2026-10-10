using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Order;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "User,Mechanic,Store")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    // =========================
    // User Orders
    // =========================

    // GET: api/orders/my
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId =
            GetCurrentUserId();

        var orders =
            await _orderService.GetMyOrdersAsync(
                userId);

        return Ok(orders);
    }

    // =========================
    // Store Orders
    // =========================

    // GET: api/orders/store
    [HttpGet("store")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> GetStoreOrders()
    {
        var userId =
            GetCurrentUserId();

        var orders =
            await _orderService.GetStoreOrdersAsync(
                userId);

        return Ok(orders);
    }

    // =========================
    // Order Details
    // =========================

    // GET: api/orders/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        int id)
    {
        var userId =
            GetCurrentUserId();

        var order =
            await _orderService.GetByIdAsync(
                id,
                userId);

        return Ok(order);
    }

    // =========================
    // Create Order
    // =========================

    // POST: api/orders
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateOrderDto dto)
    {
        var userId =
            GetCurrentUserId();

        var order =
            await _orderService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = order.Id
            },
            order);
    }

    // =========================
    // Store Changes Status
    // =========================

    // PATCH: api/orders/5/status
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateOrderStatusDto dto)
    {
        var userId =
            GetCurrentUserId();

        await _orderService.UpdateStatusAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // =========================
    // User Cancels
    // =========================

    // PATCH: api/orders/5/cancel
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(
        int id)
    {
        var userId =
            GetCurrentUserId();

        await _orderService.CancelAsync(
            id,
            userId);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !int.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "İstifadəçi məlumatı tapılmadı.");
        }

        return id;
    }
}