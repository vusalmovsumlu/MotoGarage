using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService
        _notificationService;

    public NotificationController(
        INotificationService notificationService)
    {
        _notificationService =
            notificationService;
    }

    // GET:
    // api/notifications
    //
    // api/notifications?unreadOnly=true
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();

        var notifications =
            await _notificationService
                .GetMyNotificationsAsync(
                    userId,
                    unreadOnly);

        return Ok(notifications);
    }

    // GET:
    // api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();

        var count =
            await _notificationService
                .GetUnreadCountAsync(userId);

        return Ok(new
        {
            unreadCount = count
        });
    }

    // PATCH:
    // api/notifications/5/read
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id)
    {
        var userId = GetCurrentUserId();

        await _notificationService.MarkAsReadAsync(
            id,
            userId);

        return NoContent();
    }

    // PATCH:
    // api/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        await _notificationService
            .MarkAllAsReadAsync(userId);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _notificationService.DeleteAsync(id, GetCurrentUserId());
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAll()
    {
        await _notificationService.DeleteAllAsync(GetCurrentUserId());
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
