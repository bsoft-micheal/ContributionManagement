using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.Exceptions;
using TeamContributionManagementSystem.Application.Interfaces.Common;
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
    private readonly ICurrentUserService? _currentUserService;

    public ExpenseService(
        ILogger<ExpenseService> logger,
        IExpenseRepository expenseRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _expenseRepository = expenseRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetAllAsync(string? eventName = null, string? category = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var expenses = await _expenseRepository.GetAllAsync(eventName, category, status, startDate, endDate, cancellationToken);
            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                Guid? orgUserId = null;
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
                {
                    orgUserId = parsedGuid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    orgUserId = _currentUserService.MemberId.Value;
                }

                if (orgUserId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, cancellationToken: cancellationToken);
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();

                    expenses = expenses.Where(ex =>
                        (!string.IsNullOrWhiteSpace(ex.EventName) && myEventNames.Contains(ex.EventName.Trim().ToLower())) ||
                        (!string.IsNullOrWhiteSpace(ex.SubmittedBy) && string.Equals(ex.SubmittedBy.Trim(), _currentUserService.UserName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
            }
            return expenses;
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
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken);
            if (expense == null)
            {
                var existingAny = await _expenseRepository.GetByIdWithDeletedAsync(expenseId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
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
            var existingExpenses = await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken);
            var rapidDup = existingExpenses.FirstOrDefault(e =>
                string.Equals(e.EventName, request.EventName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Category, request.Category.Trim(), StringComparison.OrdinalIgnoreCase) &&
                e.Amount == request.Amount &&
                e.CreatedOn.HasValue &&
                Math.Abs((DateTime.UtcNow - e.CreatedOn.Value).TotalSeconds) < 5);
            if (rapidDup != null)
            {
                throw new AlreadyProcessedException("This action has already been completed.");
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                Guid? orgUserId = null;
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
                {
                    orgUserId = parsedGuid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    orgUserId = _currentUserService.MemberId.Value;
                }

                if (orgUserId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, cancellationToken: cancellationToken);
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();

                    bool isMyEvent = (request.EventId.HasValue && myEventIds.Contains(request.EventId.Value)) ||
                        (!string.IsNullOrWhiteSpace(request.EventName) && myEventNames.Contains(request.EventName.Trim().ToLower()));

                    if (!isMyEvent)
                    {
                        throw new UnauthorizedAccessException("Organizers can only submit expenses for events they organize.");
                    }
                }
            }

            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventId, request.EventName, request.Category, request.Amount, null, cancellationToken);

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
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
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
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken);
            if (expense == null)
            {
                var existingAny = await _expenseRepository.GetByIdWithDeletedAsync(expenseId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            if (request.ModifiedOn.HasValue && expense.ModifiedOn.HasValue)
            {
                if (Math.Abs((expense.ModifiedOn.Value - request.ModifiedOn.Value).TotalSeconds) > 1)
                {
                    throw new RecordModifiedException();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                string.Equals(expense.Status, request.Status.Trim(), StringComparison.OrdinalIgnoreCase) &&
                !expense.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (expense.Amount == request.Amount &&
                    string.Equals(expense.EventName, request.EventName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(expense.Category, request.Category?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new AlreadyProcessedException("This action has already been completed.");
                }
            }

            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventId, request.EventName, request.Category, request.Amount, expenseId, cancellationToken);

            var attachment = !string.IsNullOrWhiteSpace(request.FileData)
                ? request.FileData.Trim()
                : (!string.IsNullOrWhiteSpace(request.FileName) ? request.FileName.Trim() : expense.FileName);

            expense.EventName = request.EventName.Trim();
            expense.Category = request.Category.Trim();
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.Status = string.IsNullOrWhiteSpace(request.Status) ? expense.Status : request.Status.Trim();
            expense.SubmittedBy = request.SubmittedBy.Trim();

            if (expense.Status.Equals(CommonConstants.ExpenseStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                expense.ApprovedBy = null;
            }
            else
            {
                expense.ApprovedBy = string.IsNullOrWhiteSpace(request.ApprovedBy) || request.ApprovedBy == "-" 
                    ? (string.IsNullOrWhiteSpace(user) ? expense.ApprovedBy : user.Trim()) 
                    : request.ApprovedBy.Trim();

                if (string.IsNullOrWhiteSpace(expense.ApprovedBy) && !string.IsNullOrWhiteSpace(user))
                {
                    expense.ApprovedBy = user.Trim();
                }
            }

            expense.Description = request.Description?.Trim() ?? string.Empty;
            expense.FileName = attachment;
            expense.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            expense.ModifiedOn = DateTime.UtcNow;

            _expenseRepository.Update(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseUpdated, expense.ExpenseId);
            return _mapper.Map<ExpenseDto>(expense);
        }
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    private async Task ValidateExpenseAmountAgainstEventBudgetAsync(
        Guid? eventId,
        string eventName,
        string? category,
        decimal requestedAmount,
        Guid? currentExpenseId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(eventName) && (!eventId.HasValue || eventId.Value == Guid.Empty)) return;

        Event? eventItem = null;
        if (eventId.HasValue && eventId.Value != Guid.Empty)
        {
            eventItem = await _eventRepository.GetByIdAsync(eventId.Value, cancellationToken);
        }

        if (eventItem == null && !string.IsNullOrWhiteSpace(eventName))
        {
            eventItem = await _eventRepository.GetByNameAsync(eventName.Trim(), category?.Trim(), cancellationToken);
        }

        if (eventItem == null) return;

        var eventDetails = await _eventRepository.GetByIdWithDetailsAsync(eventItem.EventId, cancellationToken);
        var expectedAmount = eventDetails?.TotalExpectedAmount ?? eventItem.BaseAmount;

        var existingExpenses = await _expenseRepository.GetAllAsync(eventName: eventItem.EventName, cancellationToken: cancellationToken);
        var spentAmount = existingExpenses
            .Where(e => (currentExpenseId == null || e.ExpenseId != currentExpenseId.Value) && !string.Equals(e.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.Amount);

        var remainingBudget = expectedAmount - spentAmount;
        if (expectedAmount > 0 && requestedAmount > remainingBudget)
        {
            var extraAmount = (spentAmount + requestedAmount) - expectedAmount;
            _logger.LogWarning(
                "Expense amount {RequestedAmount} exceeds the remaining budget of {RemainingBudget} for event '{EventName}'. Total projected spending exceeds expected budget by {ExtraAmount}.",
                requestedAmount,
                Math.Max(0, remainingBudget),
                eventItem.EventName,
                extraAmount);
        }
    }

    public async Task DeleteAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var expense = await _expenseRepository.GetByIdAsync(expenseId, cancellationToken);
            if (expense == null)
            {
                var existingAny = await _expenseRepository.GetByIdWithDeletedAsync(expenseId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            _expenseRepository.Delete(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseDeleted, expenseId);
        }
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
