using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Store;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(
        IStoreService storeService)
    {
        _storeService = storeService;
    }

    // GET:
    // api/stores
    // api/stores?search=parts&address=Baku
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? address)
    {
        if (!string.IsNullOrWhiteSpace(search) ||
            !string.IsNullOrWhiteSpace(address))
        {
            var stores =
                await _storeService.SearchAsync(
                    search,
                    address);

            return Ok(stores);
        }

        var result =
            await _storeService.GetAllAsync();

        return Ok(result);
    }

    // GET: api/stores/motoparts-baku
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(
        string slug)
    {
        var store =
            await _storeService.GetBySlugAsync(
                slug);

        return Ok(store);
    }

    // GET: api/stores/my-profile
    [HttpGet("my-profile")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId =
            GetCurrentUserId();

        var store =
            await _storeService.GetMyProfileAsync(
                userId);

        return Ok(store);
    }

    // POST: api/stores
    [HttpPost]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> Create(
        CreateStoreDto dto)
    {
        var userId =
            GetCurrentUserId();

        var store =
            await _storeService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetBySlug),
            new
            {
                slug = store.Slug
            },
            store);
    }

    // PUT: api/stores/my-profile
    [HttpPut("my-profile")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> UpdateMyProfile(
        UpdateStoreDto dto)
    {
        var userId =
            GetCurrentUserId();

        await _storeService.UpdateAsync(
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/stores/my-profile
    [HttpDelete("my-profile")]
    [Authorize(Roles = "Store")]
    public async Task<IActionResult> DeleteMyProfile()
    {
        var userId =
            GetCurrentUserId();

        await _storeService.DeleteAsync(
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