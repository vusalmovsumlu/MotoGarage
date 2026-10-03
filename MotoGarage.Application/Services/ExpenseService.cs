using AutoMapper;
using MotoGarage.Application.DTOs.Expense;
using MotoGarage.Application.Interfaces.Repository;
using MotoGarage.Application.Interfaces.Services;
using MotoGarage.Domain.Entities;
using MotoGarage.Domain.Exceptions;

namespace MotoGarage.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _expenseRepository = expenseRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ExpenseDto>> GetByVehicleIdAsync(
        int vehicleId,
        int userId)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        var expenses =
            await _expenseRepository
                .GetByVehicleIdAsync(vehicleId);

        return _mapper.Map<IEnumerable<ExpenseDto>>(expenses);
    }

    public async Task<ExpenseDto> GetByIdAsync(
        int id,
        int userId)
    {
        var expense =
            await _expenseRepository.GetByIdAsync(id);

        if (expense is null)
        {
            throw new KeyNotFoundException(
                "Xərc qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            expense.VehicleId,
            userId);

        return _mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseSummaryDto> GetByDateRangeAsync(
        int vehicleId,
        int userId,
        DateTime startDate,
        DateTime endDate)
    {
        await ValidateVehicleOwnershipAsync(
            vehicleId,
            userId);

        if (startDate > endDate)
        {
            throw new DomainException(
                "Başlanğıc tarixi son tarixdən böyük ola bilməz.");
        }

        var expenses =
            await _expenseRepository.GetByDateRangeAsync(
                vehicleId,
                startDate,
                endDate);

        var expenseDtos =
            _mapper.Map<IEnumerable<ExpenseDto>>(expenses)
                .ToList();

        return new ExpenseSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalAmount = expenseDtos.Sum(x => x.Amount),
            Expenses = expenseDtos
        };
    }

    public async Task<ExpenseDto> CreateAsync(
        int userId,
        CreateExpenseDto dto)
    {
        await ValidateVehicleOwnershipAsync(
            dto.VehicleId,
            userId);

        var expense =
            _mapper.Map<Expense>(dto);

        await _expenseRepository.AddAsync(expense);

        await _expenseRepository.SaveChangesAsync();

        return _mapper.Map<ExpenseDto>(expense);
    }

    public async Task UpdateAsync(
        int id,
        int userId,
        UpdateExpenseDto dto)
    {
        var expense =
            await _expenseRepository.GetByIdAsync(id);

        if (expense is null)
        {
            throw new KeyNotFoundException(
                "Xərc qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            expense.VehicleId,
            userId);

        _mapper.Map(dto, expense);

        await _expenseRepository.UpdateAsync(expense);

        await _expenseRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        int id,
        int userId)
    {
        var expense =
            await _expenseRepository.GetByIdAsync(id);

        if (expense is null)
        {
            throw new KeyNotFoundException(
                "Xərc qeydi tapılmadı.");
        }

        await ValidateVehicleOwnershipAsync(
            expense.VehicleId,
            userId);

        await _expenseRepository.DeleteAsync(expense);

        await _expenseRepository.SaveChangesAsync();
    }

    private async Task ValidateVehicleOwnershipAsync(
        int vehicleId,
        int userId)
    {
        var vehicle =
            await _vehicleRepository.GetByIdAsync(vehicleId);

        if (vehicle is null)
        {
            throw new KeyNotFoundException(
                "Nəqliyyat vasitəsi tapılmadı.");
        }

        if (vehicle.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "Bu nəqliyyat vasitəsinə giriş icazən yoxdur.");
        }
    }
}