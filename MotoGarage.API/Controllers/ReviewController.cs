using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Review;
using MotoGarage.Application.Interfaces.Services;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(
        IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // GET: api/reviews/mechanic/5
    [HttpGet("mechanic/{mechanicId}")]
    public async Task<IActionResult> GetByMechanicId(
        int mechanicId)
    {
        var reviews =
            await _reviewService.GetByMechanicIdAsync(
                mechanicId);

        return Ok(reviews);
    }

    // GET: api/reviews/10
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var review =
            await _reviewService.GetByIdAsync(id);

        return Ok(review);
    }

    // POST: api/reviews
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateReviewDto dto)
    {
        var userId = GetCurrentUserId();

        var review =
            await _reviewService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = review.Id },
            review);
    }

    // PUT: api/reviews/10
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateReviewDto dto)
    {
        var userId = GetCurrentUserId();

        await _reviewService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/reviews/10
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        var userId = GetCurrentUserId();

        await _reviewService.DeleteAsync(
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