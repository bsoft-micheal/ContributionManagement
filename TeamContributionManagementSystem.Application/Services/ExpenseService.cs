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
                        !string.IsNullOrWhiteSpace(ex.EventName) && myEventNames.Contains(ex.EventName.Trim().ToLower())
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

                    if (string.IsNullOrWhiteSpace(expense.EventName) || !myEventNames.Contains(expense.EventName.Trim().ToLower()))
                    {
                        throw new UnauthorizedAccessException("Organizers can only view expenses for events they created.");
                    }
                }
            }

            return MapToExpenseDtoWithMetadata(expense);
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
            // Validate any additional expense rows
            if (request.AdditionalExpenses != null && request.AdditionalExpenses.Count > 0)
            {
                foreach (var extra in request.AdditionalExpenses)
                {
                    if (string.IsNullOrWhiteSpace(extra.Reason))
                    {
                        throw new InvalidOperationException("Reason is required for each additional expense row.");
                    }
                    if (extra.Amount <= 0)
                    {
                        throw new InvalidOperationException("Amount must be greater than zero for each additional expense row.");
                    }
                }
            }

            var validAdditionalItems = request.AdditionalExpenses?
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason) || x.Amount > 0)
                .ToList();
            var extraSum = validAdditionalItems?.Sum(x => x.Amount) ?? (request.ExtraAmount ?? 0m);
            var budgetPart = request.BudgetAmount ?? Math.Max(0m, request.Amount - extraSum);
            var calculatedTotal = budgetPart + extraSum;

            if (calculatedTotal <= 0 && request.Amount > 0)
            {
                calculatedTotal = request.Amount;
                budgetPart = Math.Max(0m, calculatedTotal - extraSum);
            }

            if (calculatedTotal <= 0)
            {
                throw new InvalidOperationException("Total expense amount must be greater than zero.");
            }

            request.BudgetAmount = budgetPart;
            request.ExtraAmount = extraSum;
            request.Amount = calculatedTotal;

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
                        throw new UnauthorizedAccessException("Organizers can only submit expenses for events they created.");
                    }
                }
            }

            // Strictly validate main budget amount against remaining event budget (allows multiple entries up to remaining budget)
            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventId, request.EventName, request.Category, budgetPart, null, cancellationToken);

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

            string? additionalExpensesJson = null;
            if (validAdditionalItems != null && validAdditionalItems.Count > 0)
            {
                additionalExpensesJson = System.Text.Json.JsonSerializer.Serialize(validAdditionalItems);
            }

            ExpenseMetadataHelper.UnpackDescription(request.Description, calculatedTotal, out var cleanDescription, out _, out _, out _);
            if (string.IsNullOrWhiteSpace(cleanDescription) && !string.IsNullOrWhiteSpace(request.Description) && !request.Description.Contains("EXTRA_EXPENSES"))
            {
                cleanDescription = request.Description.Trim();
            }

            var effectiveDate = request.ExpenseDate == default ? DateTime.UtcNow : request.ExpenseDate;
            var expenseNumber = await _expenseRepository.GetNextExpenseNumberAsync(effectiveDate, cancellationToken);

            var expense = new Expense
            {
                ExpenseId = Guid.NewGuid(),
                ExpenseNo = expenseNumber,
                EventName = request.EventName.Trim(),
                Category = request.Category.Trim(),
                Amount = calculatedTotal,
                BudgetAmount = budgetPart,
                ExtraAmount = extraSum,
                AdditionalExpenses = additionalExpensesJson,
                ExpenseDate = request.ExpenseDate,
                Status = status ?? "Pending",
                SubmittedBy = request.SubmittedBy.Trim(),
                ApprovedBy = request.ApprovedBy?.Trim(),
                Description = cleanDescription,
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
            return MapToExpenseDtoWithMetadata(expense);
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

                    bool isExistingExpenseAllowed = !string.IsNullOrWhiteSpace(expense.EventName) && myEventNames.Contains(expense.EventName.Trim().ToLower());
                    bool isNewEventAllowed = (request.EventId.HasValue && myEventIds.Contains(request.EventId.Value)) ||
                        (!string.IsNullOrWhiteSpace(request.EventName) && myEventNames.Contains(request.EventName.Trim().ToLower()));

                    if (!isExistingExpenseAllowed || !isNewEventAllowed)
                    {
                        throw new UnauthorizedAccessException("Organizers can only edit expenses for events they created.");
                    }
                }
            }

            if (request.ModifiedOn.HasValue && expense.ModifiedOn.HasValue)
            {
                if (Math.Abs((expense.ModifiedOn.Value - request.ModifiedOn.Value).TotalSeconds) > 1)
                {
                    throw new RecordModifiedException();
                }
            }

            // Validate any additional expense rows
            if (request.AdditionalExpenses != null && request.AdditionalExpenses.Count > 0)
            {
                foreach (var extra in request.AdditionalExpenses)
                {
                    if (string.IsNullOrWhiteSpace(extra.Reason))
                    {
                        throw new InvalidOperationException("Reason is required for each additional expense row.");
                    }
                    if (extra.Amount <= 0)
                    {
                        throw new InvalidOperationException("Amount must be greater than zero for each additional expense row.");
                    }
                }
            }

            var validAdditionalItems = request.AdditionalExpenses?
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason) || x.Amount > 0)
                .ToList();

            decimal extraSum;
            string? additionalExpensesJson;
            if (request.AdditionalExpenses != null)
            {
                extraSum = validAdditionalItems?.Sum(x => x.Amount) ?? (request.ExtraAmount ?? 0m);
                additionalExpensesJson = (validAdditionalItems != null && validAdditionalItems.Count > 0)
                    ? System.Text.Json.JsonSerializer.Serialize(validAdditionalItems)
                    : null;
            }
            else
            {
                extraSum = request.ExtraAmount ?? expense.ExtraAmount;
                additionalExpensesJson = expense.AdditionalExpenses;
            }

            decimal budgetPart;
            if (request.BudgetAmount.HasValue && request.BudgetAmount.Value > 0)
            {
                budgetPart = request.BudgetAmount.Value;
            }
            else if (request.Amount > extraSum)
            {
                budgetPart = request.Amount - extraSum;
            }
            else if (expense.BudgetAmount > 0)
            {
                budgetPart = expense.BudgetAmount;
            }
            else
            {
                budgetPart = Math.Max(0m, request.Amount - extraSum);
            }

            var calculatedTotal = budgetPart + extraSum;
            if (calculatedTotal <= 0 && request.Amount > 0)
            {
                calculatedTotal = request.Amount;
                budgetPart = Math.Max(0m, calculatedTotal - extraSum);
            }

            if (calculatedTotal <= 0)
            {
                throw new InvalidOperationException("Total expense amount must be greater than zero.");
            }

            request.BudgetAmount = budgetPart;
            request.ExtraAmount = extraSum;
            request.Amount = calculatedTotal;

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                string.Equals(expense.Status, request.Status.Trim(), StringComparison.OrdinalIgnoreCase) &&
                !expense.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (expense.Amount == calculatedTotal &&
                    string.Equals(expense.EventName, request.EventName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(expense.Category, request.Category?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new AlreadyProcessedException("This action has already been completed.");
                }
            }

            // Strictly validate main budget amount against remaining event budget (excluding this expense)
            await ValidateExpenseAmountAgainstEventBudgetAsync(request.EventId, request.EventName, request.Category, budgetPart, expenseId, cancellationToken);

            var attachment = !string.IsNullOrWhiteSpace(request.FileData)
                ? request.FileData.Trim()
                : (!string.IsNullOrWhiteSpace(request.FileName) ? request.FileName.Trim() : expense.FileName);

            ExpenseMetadataHelper.UnpackDescription(request.Description, calculatedTotal, out var cleanDescription, out _, out _, out _);
            if (string.IsNullOrWhiteSpace(cleanDescription) && !string.IsNullOrWhiteSpace(request.Description) && !request.Description.Contains("EXTRA_EXPENSES"))
            {
                cleanDescription = request.Description.Trim();
            }

            expense.EventName = request.EventName.Trim();
            expense.Category = request.Category.Trim();
            expense.Amount = calculatedTotal;
            expense.BudgetAmount = budgetPart;
            expense.ExtraAmount = extraSum;
            expense.AdditionalExpenses = additionalExpensesJson;
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

            expense.Description = cleanDescription;
            expense.FileName = attachment;
            expense.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            expense.ModifiedOn = DateTime.UtcNow;

            _expenseRepository.Update(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Expenses.ExpenseUpdated, expense.ExpenseId);
            return MapToExpenseDtoWithMetadata(expense);
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

    private ExpenseDto MapToExpenseDtoWithMetadata(Expense expense)
    {
        var dto = _mapper.Map<ExpenseDto>(expense);
        ExpenseMetadataHelper.UnpackDescription(
            expense.Description,
            expense.Amount,
            out var cleanDesc,
            out var legacyBudgetAmt,
            out var legacyExtraAmt,
            out var legacyAddRows);

        dto.Description = cleanDesc;

        if (expense.BudgetAmount > 0 || expense.ExtraAmount > 0 || !string.IsNullOrWhiteSpace(expense.AdditionalExpenses))
        {
            dto.BudgetAmount = expense.BudgetAmount;
            dto.ExtraAmount = expense.ExtraAmount;

            if (!string.IsNullOrWhiteSpace(expense.AdditionalExpenses))
            {
                try
                {
                    dto.AdditionalExpenses = System.Text.Json.JsonSerializer.Deserialize<List<ExpenseAdditionalItemDto>>(
                        expense.AdditionalExpenses,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<ExpenseAdditionalItemDto>();
                }
                catch
                {
                    dto.AdditionalExpenses = legacyAddRows;
                }
            }
            else
            {
                dto.AdditionalExpenses = legacyAddRows;
            }
        }
        else
        {
            dto.BudgetAmount = legacyBudgetAmt;
            dto.ExtraAmount = legacyExtraAmt;
            dto.AdditionalExpenses = legacyAddRows;
        }

        return dto;
    }

    private async Task ValidateExpenseAmountAgainstEventBudgetAsync(
        Guid? eventId,
        string eventName,
        string? category,
        decimal requestedBudgetAmount,
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
        var spentBudgetAmount = existingExpenses
            .Where(e => (currentExpenseId == null || e.ExpenseId != currentExpenseId.Value) && !string.Equals(e.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Sum(e => e.BudgetAmount ?? e.Amount);

        var remainingBudget = Math.Max(0m, expectedAmount - spentBudgetAmount);

        if (expectedAmount > 0 && requestedBudgetAmount > remainingBudget)
        {
            throw new InvalidOperationException(
                $"The main expense amount (₹{requestedBudgetAmount:N0}) exceeds the remaining expected budget of ₹{remainingBudget:N0} for event '{eventItem.EventName}'. Please enter a budget amount up to ₹{remainingBudget:N0} and record any extra spending using 'Add Row'.");
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

                    if (string.IsNullOrWhiteSpace(expense.EventName) || !myEventNames.Contains(expense.EventName.Trim().ToLower()))
                    {
                        throw new UnauthorizedAccessException("Organizers can only delete expenses for events they created.");
                    }
                }
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
