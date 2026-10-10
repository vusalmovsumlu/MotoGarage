using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Enums;
using MotoGarage.Application.DTOs.Admin;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IBusinessApplicationService _applications;

    public AdminController(
        IAdminService adminService, IBusinessApplicationService applications)
    {
        _adminService = adminService;
        _applications = applications;
    }

    // GET:
    // api/admin/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard =
            await _adminService.GetDashboardAsync();

        return Ok(dashboard);
    }

    // GET:
    // api/admin/users
    //
    // api/admin/users?search=mahammad
    //
    // api/admin/users?role=Store
    //
    // api/admin/users?search=ali&role=Mechanic
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserRole? role)
    {
        var users =
            await _adminService.GetUsersAsync(
                search,
                role);

        return Ok(users);
    }

    // PATCH:
    // api/admin/users/5/block
    [HttpPatch("users/{id}/block")]
    public async Task<IActionResult> BlockUser(
        int id)
    {
        var adminId =
            GetCurrentUserId();

        await _adminService.BlockUserAsync(
            id,
            adminId);

        return NoContent();
    }

    // PATCH:
    // api/admin/users/5/unblock
    [HttpPatch("users/{id}/unblock")]
    public async Task<IActionResult> UnblockUser(
        int id)
    {
        await _adminService.UnblockUserAsync(id);

        return NoContent();
    }

    [HttpGet("business-applications")]
    public async Task<IActionResult> GetApplications([FromQuery] BusinessApprovalStatus? status = BusinessApprovalStatus.Pending) =>
        Ok(await _applications.GetApplicationsAsync(status));

    [HttpDelete("users/{id:int:min(1)}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _adminService.DeleteUserAsync(id, GetCurrentUserId());
        return NoContent();
    }

    [HttpPost("business-applications/{id:int}/accept")]
    public async Task<IActionResult> AcceptApplication(int id)
    {
        await _applications.ReviewAsync(id, GetCurrentUserId(), true, null);
        return NoContent();
    }

    [HttpPost("business-applications/{id:int}/decline")]
    public async Task<IActionResult> DeclineApplication(int id, RejectBusinessApplicationDto dto)
    {
        await _applications.ReviewAsync(id, GetCurrentUserId(), false, dto.Comment);
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
