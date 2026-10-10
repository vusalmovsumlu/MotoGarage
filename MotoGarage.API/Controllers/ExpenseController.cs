using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoGarage.Application.DTOs.Expense;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Exceptions;
using System.Security.Claims;

namespace MotoGarage.API.Controllers;

[ApiController]
[Route("api/expenses")]
[Authorize(Roles = "User,Mechanic,Store")]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpenseController(
        IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    // GET:
    // api/expenses?vehicleId=5
    //
    // GET:
    // api/expenses?vehicleId=5
    // &startDate=2026-09-01
    // &endDate=2026-10-01
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int vehicleId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();

        if (startDate.HasValue != endDate.HasValue)
        {
            throw new DomainException(
                "Başlanğıc və bitiş tarixləri birlikdə göndərilməlidir.");
        }

        if (startDate.HasValue && endDate.HasValue)
        {
            var summary =
                await _expenseService.GetByDateRangeAsync(
                    vehicleId,
                    userId,
                    startDate.Value,
                    endDate.Value);

            return Ok(summary);
        }

        var expenses =
            await _expenseService.GetByVehicleIdAsync(
                vehicleId,
                userId);

        return Ok(expenses);
    }

    // GET: api/expenses/10
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        var expense =
            await _expenseService.GetByIdAsync(
                id,
                userId);

        return Ok(expense);
    }

    // POST: api/expenses
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateExpenseDto dto)
    {
        var userId = GetCurrentUserId();

        var expense =
            await _expenseService.CreateAsync(
                userId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = expense.Id },
            expense);
    }

    // PUT: api/expenses/10
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateExpenseDto dto)
    {
        var userId = GetCurrentUserId();

        await _expenseService.UpdateAsync(
            id,
            userId,
            dto);

        return NoContent();
    }

    // DELETE: api/expenses/10
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        await _expenseService.DeleteAsync(
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