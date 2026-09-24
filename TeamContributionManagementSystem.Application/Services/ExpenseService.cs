using System.IO;
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

            if (!string.IsNullOrWhiteSpace(expense.FileName) && expense.FileName.StartsWith("/expense_attachments/"))
            {
                var relativePath = expense.FileName.Split('?')[0].TrimStart('/');
                var candidatePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath),
                    Path.Combine(AppContext.BaseDirectory, "wwwroot", relativePath),
                    Path.Combine(@"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot", relativePath)
                };

                foreach (var p in candidatePaths.Distinct())
                {
                    if (File.Exists(p))
                    {
                        try { File.Delete(p); } catch { }
                    }
                }
            }

            _expenseRepository.Delete(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }

    private async Task<string?> ProcessAttachmentAsync(string? originalFileName, string? fileData, string? existingFilePath, CancellationToken cancellationToken)
    {
        // 1. If new fileData provided in base64 format (e.g. data:image/png;base64,... or raw base64)
        if (!string.IsNullOrWhiteSpace(fileData) && (fileData.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || fileData.Length > 100))
        {
            // Delete old file if existed on server
            if (!string.IsNullOrWhiteSpace(existingFilePath) && existingFilePath.StartsWith("/expense_attachments/"))
            {
                var oldRel = existingFilePath.Split('?')[0].TrimStart('/');
                var candidateOldPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRel),
                    Path.Combine(AppContext.BaseDirectory, "wwwroot", oldRel),
                    Path.Combine(@"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot", oldRel)
                };

                foreach (var p in candidateOldPaths.Distinct())
                {
                    if (File.Exists(p))
                    {
                        try { File.Delete(p); } catch { }
                    }
                }
            }

            var commaIdx = fileData.IndexOf(',');
            var base64Part = commaIdx >= 0 ? fileData.Substring(commaIdx + 1) : fileData;
            var bytes = Convert.FromBase64String(base64Part);

            // Determine extension
            var ext = ".jpg";
            if (fileData.Contains("image/png", StringComparison.OrdinalIgnoreCase)) ext = ".png";
            else if (fileData.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) || fileData.Contains("image/jpg", StringComparison.OrdinalIgnoreCase)) ext = ".jpg";
            else if (fileData.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) ext = ".webp";
            else if (fileData.Contains("application/pdf", StringComparison.OrdinalIgnoreCase)) ext = ".pdf";
            else if (!string.IsNullOrWhiteSpace(originalFileName))
            {
                var origExt = Path.GetExtension(originalFileName);
                if (!string.IsNullOrWhiteSpace(origExt)) ext = origExt;
            }

            var cleanName = !string.IsNullOrWhiteSpace(originalFileName)
                ? Path.GetFileNameWithoutExtension(originalFileName).Replace(" ", "_").ToLowerInvariant()
                : "receipt";

            cleanName = string.Concat(cleanName.Split(Path.GetInvalidFileNameChars()));
            if (cleanName.Length > 25) cleanName = cleanName.Substring(0, 25);

            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{dateStr}_{cleanName}{ext}";

            var targetDirectories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "expense_attachments"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "expense_attachments"),
                Path.Combine(@"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot", "expense_attachments")
            };

            foreach (var dir in targetDirectories.Distinct())
            {
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    var fullPath = Path.Combine(dir, uniqueFileName);
                    await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write attachment to {Directory}", dir);
                }
            }

            return $"/expense_attachments/{uniqueFileName}";
        }

        // 2. If file was cleared (both fileName and fileData null or empty)
        if (string.IsNullOrWhiteSpace(originalFileName) && string.IsNullOrWhiteSpace(fileData))
        {
            if (!string.IsNullOrWhiteSpace(existingFilePath) && existingFilePath.StartsWith("/expense_attachments/"))
            {
                var oldRel = existingFilePath.Split('?')[0].TrimStart('/');
                var candidateOldPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldRel),
                    Path.Combine(AppContext.BaseDirectory, "wwwroot", oldRel),
                    Path.Combine(@"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot", oldRel)
                };

                foreach (var p in candidateOldPaths.Distinct())
                {
                    if (File.Exists(p))
                    {
                        try { File.Delete(p); } catch { }
                    }
                }
            }
            return null;
        }

        // 3. Keep previous file if no new data was uploaded
        return !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : existingFilePath;
    }
}
