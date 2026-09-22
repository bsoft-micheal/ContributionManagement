using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(IExpenseRepository expenseRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var expenses = await _expenseRepository.GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<ExpenseDto>>(expenses);
    }

    public async Task<ExpenseDto> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");
        return _mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var expense = new Expense
        {
            ExpenseId = Guid.NewGuid(),
            EventName = request.EventName.Trim(),
            Category = request.Category.Trim(),
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim(),
            SubmittedBy = request.SubmittedBy.Trim(),
            ApprovedBy = request.ApprovedBy?.Trim(),
            Description = request.Description.Trim(),
            FileName = request.FileName?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? "System" : user,
            CreatedOn = DateTime.UtcNow
        };

        if (expense.Status == "Approved" && string.IsNullOrWhiteSpace(expense.ApprovedBy))
        {
            expense.ApprovedBy = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
        }

        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ExpenseDto>(expense);
    }

    public async Task<ExpenseDto> UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");

        expense.EventName = request.EventName.Trim();
        expense.Category = request.Category.Trim();
        expense.Amount = request.Amount;
        expense.ExpenseDate = request.ExpenseDate;
        expense.Status = string.IsNullOrWhiteSpace(request.Status) ? expense.Status : request.Status.Trim();
        expense.SubmittedBy = request.SubmittedBy.Trim();
        expense.ApprovedBy = request.ApprovedBy?.Trim();
        expense.Description = request.Description.Trim();
        expense.FileName = request.FileName?.Trim() ?? expense.FileName;
        expense.ModifiedBy = string.IsNullOrWhiteSpace(user) ? "System" : user;
        expense.ModifiedOn = DateTime.UtcNow;

        if (expense.Status == "Approved" && string.IsNullOrWhiteSpace(expense.ApprovedBy))
        {
            expense.ApprovedBy = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
        }

        _expenseRepository.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ExpenseDto>(expense);
    }

    public async Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");

        _expenseRepository.Delete(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
