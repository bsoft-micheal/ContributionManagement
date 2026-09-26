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
    private readonly IStatusRepository? _statusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(
        ILogger<ExpenseService> logger,
        IExpenseRepository expenseRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null)
    {
        _logger = logger;
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var expenses = await _expenseRepository.GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<ExpenseDto>>(expenses);
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
            var savedFilePath = await ProcessAttachmentAsync(request.FileName, request.FileData, null, cancellationToken);

            var status = request.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status) && _statusRepository != null)
            {
                var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                status = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Pending", StringComparison.OrdinalIgnoreCase))?.StatusName
                    ?? dbStatuses.FirstOrDefault()?.StatusName
                    ?? string.Empty;
            }

            var expense = new Expense
            {
                ExpenseId = Guid.NewGuid(),
                EventName = request.EventName.Trim(),
                Category = request.Category.Trim(),
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate,
                Status = status ?? string.Empty,
                SubmittedBy = request.SubmittedBy.Trim(),
                ApprovedBy = request.ApprovedBy?.Trim(),
                Description = request.Description.Trim(),
                FileName = savedFilePath,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
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

            var savedFilePath = await ProcessAttachmentAsync(request.FileName, request.FileData, expense.FileName, cancellationToken);

            expense.EventName = request.EventName.Trim();
            expense.Category = request.Category.Trim();
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Status = string.IsNullOrWhiteSpace(request.Status) ? expense.Status : request.Status.Trim();
            expense.SubmittedBy = request.SubmittedBy.Trim();
            expense.ApprovedBy = request.ApprovedBy?.Trim();
            expense.Description = request.Description.Trim();
            expense.FileName = savedFilePath;
            expense.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            expense.ModifiedOn = DateTime.UtcNow;

            if (expense.Status == CommonConstants.ExpenseStatuses.Approved && string.IsNullOrWhiteSpace(expense.ApprovedBy))
            {
                expense.ApprovedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
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

    private Task<string?> ProcessAttachmentAsync(string? originalFileName, string? fileData, string? existingFilePath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(fileData) && (fileData.StartsWith(CommonConstants.Defaults.DataUriPrefix, StringComparison.OrdinalIgnoreCase) || fileData.Length > 100))
        {
            return Task.FromResult<string?>(fileData);
        }

        if (!string.IsNullOrWhiteSpace(originalFileName) && (originalFileName.StartsWith(CommonConstants.Defaults.DataUriPrefix, StringComparison.OrdinalIgnoreCase) || originalFileName.Length > 100))
        {
            return Task.FromResult<string?>(originalFileName);
        }

        if (string.IsNullOrWhiteSpace(originalFileName) && string.IsNullOrWhiteSpace(fileData))
        {
            return Task.FromResult<string?>(null);
        }

        var result = !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : existingFilePath;
        return Task.FromResult<string?>(result);
    }
}
