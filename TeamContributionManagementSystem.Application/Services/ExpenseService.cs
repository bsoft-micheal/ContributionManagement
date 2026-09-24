using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly ILogger<ExpenseService> _logger;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(ILogger<ExpenseService> logger, IExpenseRepository expenseRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<ExpenseDto> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var savedFilePath = await ProcessAttachmentAsync(request.FileName, request.FileData, null, cancellationToken);

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
                FileName = savedFilePath,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task<ExpenseDto> UpdateAsync(Guid expenseId, UpdateExpenseRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken)
                ?? throw new KeyNotFoundException($"Expense with ID {expenseId} not found.");

            _expenseRepository.Delete(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }

    private Task<string?> ProcessAttachmentAsync(string? originalFileName, string? fileData, string? existingFilePath, CancellationToken cancellationToken)
    {
        // 1. If new fileData provided in base64 format (data:image/...), store base64 string directly in DB
        if (!string.IsNullOrWhiteSpace(fileData) && (fileData.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || fileData.Length > 100))
        {
            return Task.FromResult<string?>(fileData);
        }

        // 2. If originalFileName is already a base64 string
        if (!string.IsNullOrWhiteSpace(originalFileName) && (originalFileName.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || originalFileName.Length > 100))
        {
            return Task.FromResult<string?>(originalFileName);
        }

        // 3. If file was cleared (both fileName and fileData null or empty)
        if (string.IsNullOrWhiteSpace(originalFileName) && string.IsNullOrWhiteSpace(fileData))
        {
            return Task.FromResult<string?>(null);
        }

        // 4. Retain previous base64 or file value
        var result = !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : existingFilePath;
        return Task.FromResult<string?>(result);
    }
}
