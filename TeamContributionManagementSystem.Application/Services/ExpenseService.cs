using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly ILogger<ExpenseService> _logger;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IStatusRepository? _statusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(
        ILogger<ExpenseService> logger,
        IExpenseRepository expenseRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null)
    {
        _logger = logger;
        _expenseRepository = expenseRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _expenseRepository.GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<ExpenseDto> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Expenses.NotFound);
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventName, request.Amount, null, cancellationToken);

            var attachment = !string.IsNullOrWhiteSpace(request.FileData)
                ? request.FileData.Trim()
                : request.FileName?.Trim();

            var status = request.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status) && _statusRepository != null)
            {
                var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                status = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Pending", StringComparison.OrdinalIgnoreCase))?.StatusName
                    ?? dbStatuses.FirstOrDefault()?.StatusName
                    ?? "Pending";
            }
            else if (string.IsNullOrWhiteSpace(status))
            {
                status = "Pending";
            }

            var expense = new Expense
            {
                ExpenseId = Guid.NewGuid(),
                EventName = request.EventName.Trim(),
                Category = request.Category.Trim(),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                Status = status ?? "Pending",
                SubmittedBy = request.SubmittedBy.Trim(),
                ApprovedBy = request.ApprovedBy?.Trim(),
                Description = request.Description.Trim(),
                FileName = attachment,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow
            };

            if (expense.Status == CommonConstants.ExpenseStatuses.Approved && string.IsNullOrWhiteSpace(expense.ApprovedBy))
            {
                expense.ApprovedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            }

            await _expenseRepository.AddAsync(expense, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseCreated, expense.ExpenseId, expense.Amount);
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<ExpenseDto> UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Expenses.NotFound);

            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventName, request.Amount, expenseId, cancellationToken);

            var attachment = !string.IsNullOrWhiteSpace(request.FileData)
                ? request.FileData.Trim()
                : (!string.IsNullOrWhiteSpace(request.FileName) ? request.FileName.Trim() : expense.FileName);

            expense.EventName = request.EventName.Trim();
            expense.Category = request.Category.Trim();
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Status = string.IsNullOrWhiteSpace(request.Status) ? expense.Status : request.Status.Trim();
            expense.SubmittedBy = request.SubmittedBy.Trim();
            expense.ApprovedBy = string.IsNullOrWhiteSpace(request.ApprovedBy) || request.ApprovedBy == "-" 
                ? (string.IsNullOrWhiteSpace(user) ? expense.ApprovedBy : user.Trim()) 
                : request.ApprovedBy.Trim();
            expense.Description = request.Description.Trim();
            expense.FileName = attachment;
            expense.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            expense.ModifiedOn = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(expense.ApprovedBy) && !string.IsNullOrWhiteSpace(user))
            {
                expense.ApprovedBy = user.Trim();
            }

            _expenseRepository.Update(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseUpdated, expense.ExpenseId);
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    private async Task ValidateExpenseAmountAgainstEventBudgetAsync(string eventName, decimal requestedAmount, Guid? currentExpenseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventName)) return;

        var eventItem = await _eventRepository.GetByNameAsync(eventName.Trim(), cancellationToken);
        if (eventItem == null) return;

        var eventDetails = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId, cancellationToken);
        var expectedAmount = eventDetails?.TotalExpectedAmount ?? eventItem.BaseAmount;

        var existingExpenses = await _expenseRepository.GetAllAsync(eventName: eventItem.EventName, cancellationToken: cancellationToken);
        var spentAmount = existingExpenses
            .Where(e => (currentExpenseId == null || e.ExpenseId != currentExpenseId.Value) && !string.Equals(e.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.Amount);

        var remainingBudget = expectedAmount - spentAmount;
        if (requestedAmount > remainingBudget)
        {
            throw new InvalidOperationException(string.Format(
                CommonMessages.Expenses.ExceedsExpectedBudgetFormat,
                expectedAmount,
                spentAmount,
                Math.Max(0, remainingBudget)));
        }
    }

    public async Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Expenses.NotFound);

            _expenseRepository.Delete(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseDeleted, expenseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
