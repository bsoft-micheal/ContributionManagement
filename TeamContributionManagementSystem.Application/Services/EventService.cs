using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QRCoder;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Application.Exceptions;

namespace TeamContributionManagementSystem.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IBudgetCalculationRepository _budgetCalculationRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IGalleryRepository _galleryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IStatusRepository? _statusRepository;

    public EventService(
        IEventRepository eventRepository,
        IEventTypeRepository eventTypeRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository,
        IContributionRepository contributionRepository,
        IBudgetCalculationRepository budgetCalculationRepository,
        IExpenseRepository expenseRepository,
        IGalleryRepository galleryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        ILogger<EventService> logger,
        IServiceScopeFactory serviceScopeFactory,
        ICurrentUserService? currentUserService = null,
        IStatusRepository? statusRepository = null)
    {
        _eventRepository = eventRepository;
        _eventTypeRepository = eventTypeRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _contributionRepository = contributionRepository;
        _budgetCalculationRepository = budgetCalculationRepository;
        _expenseRepository = expenseRepository;
        _galleryRepository = galleryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _currentUserService = currentUserService;
        _statusRepository = statusRepository;
    }

    public async Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var allExpensesRaw = _expenseRepository != null
                ? await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<TeamContributionManagementSystem.Application.DTOs.Expenses.ExpenseDto>();

            // Only count verified expenses for event totals
            var allExpenses = allExpensesRaw
                .Where(e => !string.IsNullOrWhiteSpace(e.Status) && (
                    e.Status.Contains("verif", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            List<EventSummaryDto> events;

            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                if (!myMemberId.HasValue && !string.IsNullOrWhiteSpace(_currentUserService.Email) && _memberRepository != null)
                {
                    var myMember = await _memberRepository.GetByEmailAsync(_currentUserService.Email.Trim(), cancellationToken);
                    if (myMember != null)
                    {
                        myMemberId = myMember.MemberId;
                    }
                }

                if (!myMemberId.HasValue)
                {
                    return Array.Empty<EventSummaryDto>();
                }

                var memberEvents = await _eventRepository.GetAllForMemberAsync(myMemberId.Value, month, year, cancellationToken);
                var allMonthlyEvents = await _eventRepository.GetAllAsync(month, year, cancellationToken);

                foreach (var ev in memberEvents)
                {
                    var overallEvent = allMonthlyEvents.FirstOrDefault(e => e.EventId == ev.EventId);
                    if (overallEvent != null)
                    {
                        ev.SupportTicketRaisedCount = overallEvent.SupportTicketRaisedCount;
                    }
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), ev.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);
                    var overallExpVal = overallEvent != null ? overallEvent.TotalExpectedAmount : ev.TotalExpectedAmount;
                    var eventRemaining = ev.TotalPaidAmount - eventExp;

                    ev.CollectedAmount = ev.TotalPaidAmount;
                    ev.PendingAmount = ev.TotalExpectedAmount - ev.TotalPaidAmount;
                    ev.ExpenseAmount = eventExp;
                    ev.RemainingAmount = eventRemaining;
                    ev.OverallExpectedAmount = overallExpVal;
                    ev.OverallExpenseAmount = eventExp;
                    ev.OverallRemainingAmount = eventRemaining;
                }

                events = memberEvents;
            }
            else if (_currentUserService != null && _currentUserService.IsOrganizerRole)
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
                else if (!string.IsNullOrWhiteSpace(_currentUserService.Email) && _userRepository != null)
                {
                    var u = await _userRepository.GetByEmailAsync(_currentUserService.Email.Trim(), cancellationToken);
                    if (u != null) orgUserId = u.UserId;
                }

                if (!orgUserId.HasValue)
                {
                    return Array.Empty<EventSummaryDto>();
                }

                var organizerEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, month, year, cancellationToken);
                foreach (var ev in organizerEvents)
                {
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), ev.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);
                    var eventRemaining = ev.TotalPaidAmount - eventExp;

                    ev.CollectedAmount = ev.TotalPaidAmount;
                    ev.PendingAmount = ev.TotalExpectedAmount - ev.TotalPaidAmount;
                    ev.ExpenseAmount = eventExp;
                    ev.RemainingAmount = eventRemaining;
                    ev.OverallExpectedAmount = ev.TotalExpectedAmount;
                    ev.OverallExpenseAmount = eventExp;
                    ev.OverallRemainingAmount = eventRemaining;
                }

                events = organizerEvents;
            }
            else
            {
                var monthlyEvents = await _eventRepository.GetAllAsync(month, year, cancellationToken);
                foreach (var ev in monthlyEvents)
                {
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), ev.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);
                    var eventRemaining = ev.TotalPaidAmount - eventExp;

                    ev.CollectedAmount = ev.TotalPaidAmount;
                    ev.PendingAmount = ev.TotalExpectedAmount - ev.TotalPaidAmount;
                    ev.ExpenseAmount = eventExp;
                    ev.RemainingAmount = eventRemaining;
                    ev.OverallExpectedAmount = ev.TotalExpectedAmount;
                    ev.OverallExpenseAmount = eventExp;
                    ev.OverallRemainingAmount = eventRemaining;
                }

                events = monthlyEvents;
            }

            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);
                var userDict = users.ToDictionary(
                    u => u.UserId.ToString(),
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var ev in events)
                {
                    if (string.IsNullOrWhiteSpace(ev.CreatedByName) && !string.IsNullOrWhiteSpace(ev.CreatedBy) && userDict.TryGetValue(ev.CreatedBy, out var userName))
                    {
                        ev.CreatedByName = userName;
                        ev.CreatedBy = userName;
                    }
                    else if (!string.IsNullOrWhiteSpace(ev.CreatedByName))
                    {
                        ev.CreatedBy = ev.CreatedByName;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve user names for events");
            }

            foreach (var ev in events)
            {
                if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(ev.EventDates);
                        var typeDates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var memberDates = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var mDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                foreach (var mProp in prop.Value.EnumerateObject())
                                {
                                    mDict[mProp.Name] = mProp.Value.GetString() ?? string.Empty;
                                }
                                memberDates[prop.Name] = mDict;
                                typeDates[prop.Name] = string.Join(", ", mDict.Values.Where(v => !string.IsNullOrWhiteSpace(v)));
                            }
                            else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                typeDates[prop.Name] = prop.Value.GetString() ?? string.Empty;
                            }
                        }

                        if (typeDates.Count > 0) ev.EventTypeDates = typeDates;
                        if (memberDates.Count > 0)
                        {
                            ev.EventTypeMemberDates = memberDates;
                            var typeMembers = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
                            foreach (var (typeName, mDict) in memberDates)
                            {
                                var guids = new List<Guid>();
                                foreach (var key in mDict.Keys)
                                {
                                    if (Guid.TryParse(key, out var g)) guids.Add(g);
                                }
                                if (guids.Count > 0)
                                {
                                    typeMembers[typeName] = guids;
                                    if (typeName.Equals("Marriage", StringComparison.OrdinalIgnoreCase))
                                        typeMembers["marriage"] = guids;
                                    if (typeName.Equals("Send Off", StringComparison.OrdinalIgnoreCase) || typeName.Equals("SendOff", StringComparison.OrdinalIgnoreCase))
                                        typeMembers["sendOff"] = guids;
                                }
                            }
                            if (typeMembers.Count > 0) ev.EventTypeMembers = typeMembers;
                        }

                        var allEventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
                        var discoveredTypeIds = new HashSet<Guid>();
                        if (ev.EventTypeId != Guid.Empty)
                        {
                            discoveredTypeIds.Add(ev.EventTypeId);
                        }
                        foreach (var catName in typeDates.Keys)
                        {
                            var cleanCat = catName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
                            var matched = allEventTypes.FirstOrDefault(t => t.EventTypeName.Replace(" ", "").Replace("-", "").Replace("_", "").Equals(cleanCat, StringComparison.OrdinalIgnoreCase));
                            if (matched != null) discoveredTypeIds.Add(matched.EventTypeId);
                        }
                        if (discoveredTypeIds.Count > 0)
                        {
                            ev.EventTypeIds = discoveredTypeIds.ToList();
                        }
                    }
                    catch { }
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<EventDetailsDto> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventDetails = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Events.NotFound);

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                Guid? curUserGuid = null;
                if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
                {
                    curUserGuid = parsedGuid;
                }
                else if (_currentUserService.MemberId.HasValue)
                {
                    curUserGuid = _currentUserService.MemberId.Value;
                }

                if (curUserGuid.HasValue)
                {
                    var rawEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
                    if (rawEvent != null)
                    {
                        bool isUserAdmin = _currentUserService.HasRole(CommonRoles.Admin);
                        bool isAllowed = rawEvent.CreatedBy == curUserGuid.Value &&
                            (!isUserAdmin || (rawEvent.Description != null && rawEvent.Description.Contains("CREATOR_ROLE: Organizer")));

                        if (!isAllowed)
                        {
                            throw new UnauthorizedAccessException("Organizers can only view details of events they created.");
                        }
                    }
                }
            }

            if (_expenseRepository != null)
            {
                try
                {
                    var allExpensesRaw = await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken);
                    var allExpenses = allExpensesRaw
                        .Where(e => !string.IsNullOrWhiteSpace(e.Status) && (
                            e.Status.Contains("verif", StringComparison.OrdinalIgnoreCase) ||
                            e.Status.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                            e.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    var eventExp = allExpenses
                        .Where(e => string.Equals(e.EventName?.Trim(), eventDetails.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Sum(e => e.Amount);

                    eventDetails.CollectedAmount = eventDetails.TotalPaidAmount;
                    eventDetails.PendingAmount = eventDetails.TotalExpectedAmount - eventDetails.TotalPaidAmount;
                    eventDetails.ExpenseAmount = eventExp;
                    eventDetails.RemainingAmount = eventDetails.TotalPaidAmount - eventExp;
                    eventDetails.OverallExpectedAmount = eventDetails.TotalExpectedAmount;
                    eventDetails.OverallExpenseAmount = eventExp;
                    eventDetails.OverallRemainingAmount = eventDetails.TotalPaidAmount - eventExp;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve expenses for event details");
                }
            }

            if (string.IsNullOrWhiteSpace(eventDetails.CreatedByName) && !string.IsNullOrWhiteSpace(eventDetails.CreatedBy))
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    var user = users.FirstOrDefault(u => u.UserId.ToString().Equals(eventDetails.CreatedBy, StringComparison.OrdinalIgnoreCase));
                    if (user != null)
                    {
                        var name = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Username;
                        eventDetails.CreatedByName = name;
                        eventDetails.CreatedBy = name;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve user name for event details");
                }
            }
            else if (!string.IsNullOrWhiteSpace(eventDetails.CreatedByName))
            {
                eventDetails.CreatedBy = eventDetails.CreatedByName;
            }

            if (!string.IsNullOrWhiteSpace(eventDetails.EventDates) && eventDetails.EventDates.Trim().StartsWith("{"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(eventDetails.EventDates);
                    var typeDates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var memberDates = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var mDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var mProp in prop.Value.EnumerateObject())
                            {
                                mDict[mProp.Name] = mProp.Value.GetString() ?? string.Empty;
                            }
                            memberDates[prop.Name] = mDict;
                            typeDates[prop.Name] = string.Join(", ", mDict.Values.Where(v => !string.IsNullOrWhiteSpace(v)));
                        }
                        else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            typeDates[prop.Name] = prop.Value.GetString() ?? string.Empty;
                        }
                    }

                    if (typeDates.Count > 0) eventDetails.EventTypeDates = typeDates;
                    if (memberDates.Count > 0)
                    {
                        eventDetails.EventTypeMemberDates = memberDates;
                        var typeMembers = new Dictionary<string, List<Guid>>(StringComparer.OrdinalIgnoreCase);
                        foreach (var (typeName, mDict) in memberDates)
                        {
                            var guids = new List<Guid>();
                            foreach (var key in mDict.Keys)
                            {
                                if (Guid.TryParse(key, out var g)) guids.Add(g);
                            }
                            if (guids.Count > 0)
                            {
                                typeMembers[typeName] = guids;
                                if (typeName.Equals("Marriage", StringComparison.OrdinalIgnoreCase))
                                    typeMembers["marriage"] = guids;
                                if (typeName.Equals("Send Off", StringComparison.OrdinalIgnoreCase) || typeName.Equals("SendOff", StringComparison.OrdinalIgnoreCase))
                                    typeMembers["sendOff"] = guids;
                            }
                        }
                        if (typeMembers.Count > 0) eventDetails.EventTypeMembers = typeMembers;
                    }

                    var allTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
                    var discoveredTypeIds = new HashSet<Guid>();
                    if (eventDetails.EventTypeId != Guid.Empty)
                    {
                        discoveredTypeIds.Add(eventDetails.EventTypeId);
                    }
                    foreach (var catName in typeDates.Keys)
                    {
                        var cleanCat = catName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
                        var matched = allTypes.FirstOrDefault(t => t.EventTypeName.Replace(" ", "").Replace("-", "").Replace("_", "").Equals(cleanCat, StringComparison.OrdinalIgnoreCase));
                        if (matched != null) discoveredTypeIds.Add(matched.EventTypeId);
                    }
                    if (discoveredTypeIds.Count > 0)
                    {
                        eventDetails.EventTypeIds = discoveredTypeIds.ToList();
                    }
                }
                catch { }
            }

            return eventDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<EventDetailsDto> CreateAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.EventDate.Date < DateTime.Today)
            {
                throw new InvalidOperationException(CommonMessages.Events.PastDateNotAllowed);
            }

            var user = await _userRepository.GetByIdAsync(createdByUserId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            var targetTypeId = request.EventTypeId != Guid.Empty
                ? request.EventTypeId
                : (request.EventTypeIds != null && request.EventTypeIds.Count > 0 ? request.EventTypeIds[0] : Guid.Empty);

            var eventType = await _eventTypeRepository.GetByIdAsync(targetTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            if (!eventType.IsActive)
            {
                throw new InvalidOperationException(CommonMessages.Events.InactiveEventType);
            }

            // Monthly Duplicate Event Prevention:
            // Check all requested event types across all represented month/year combinations
            var allTypesToCheck = new HashSet<Guid>();
            if (targetTypeId != Guid.Empty) allTypesToCheck.Add(targetTypeId);
            if (request.EventTypeIds != null && request.EventTypeIds.Count > 0)
            {
                foreach (var tid in request.EventTypeIds)
                {
                    if (tid != Guid.Empty) allTypesToCheck.Add(tid);
                }
            }

            var allDatesToCheck = new List<DateTime> { request.EventDate.Date };
            if (request.EventTypeDates != null)
            {
                foreach (var dVal in request.EventTypeDates.Values)
                {
                    if (DateTime.TryParse(dVal, out var parsedDate))
                    {
                        allDatesToCheck.Add(parsedDate.Date);
                    }
                }
            }
            if (request.EventTypeMemberDates != null)
            {
                foreach (var innerDict in request.EventTypeMemberDates.Values)
                {
                    foreach (var dVal in innerDict.Values)
                    {
                        if (DateTime.TryParse(dVal, out var parsedDate))
                        {
                            allDatesToCheck.Add(parsedDate.Date);
                        }
                    }
                }
            }

            var distinctMonthYears = allDatesToCheck
                .Select(d => (Month: d.Month, Year: d.Year))
                .Distinct()
                .ToList();

            // Validate Monthly Event Restrictions:
            // Rule A: Birthday is allowed only once per calendar month
            // Rule B: Non-Birthday prevents duplicate celebrants for the same event type in the same calendar month
            await ValidateMonthlyEventRulesAsync(request, eventType, null, cancellationToken);

            var participantIds = (request.ParticipantIds ?? new List<Guid>()).Distinct().ToList();
            if (participantIds.Count == 0)
            {
                throw new InvalidOperationException(CommonMessages.Events.AtLeastOneParticipantRequired);
            }

            var members = await _memberRepository.GetByIdsAsync(participantIds, cancellationToken);

            if (request.BaseAmount <= 0)
            {
                throw new InvalidOperationException("Event cannot be saved with ₹0. Please configure budget calculation rates or enter a valid contribution amount.");
            }

            decimal plannedBudget = RoundingHelper.RoundToWhole(request.BaseAmount);
            decimal effectiveBudget = plannedBudget;
            decimal allocatedAmount = 0m;
            decimal appliedAdjustment = 0m;
            string appliedAdjustmentType = "None";
            string? prevEventName = null;

            if (request.UsePreviousEventBalance)
            {
                var balanceInfo = await GetAvailableAmountInHandAsync(null, cancellationToken);
                if (balanceInfo.EligibleAdjustmentAmount > 0)
                {
                    appliedAdjustment = balanceInfo.EligibleAdjustmentAmount;
                    appliedAdjustmentType = balanceInfo.AdjustmentType;
                    prevEventName = balanceInfo.PreviousEventName;

                    if (appliedAdjustmentType == "DeficitRecovery")
                    {
                        effectiveBudget = plannedBudget + appliedAdjustment;
                        allocatedAmount = 0m;
                    }
                    else if (appliedAdjustmentType == "SurplusCredit")
                    {
                        appliedAdjustment = Math.Min(appliedAdjustment, plannedBudget);
                        allocatedAmount = appliedAdjustment;
                    }
                }
            }
            else if (request.UseAvailableBalance)
            {
                allocatedAmount = request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0
                    ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value)
                    : plannedBudget;

                if (allocatedAmount > plannedBudget)
                {
                    throw new InvalidOperationException($"The deducted balance amount (₹{allocatedAmount:N0}) cannot exceed the planned budget (₹{plannedBudget:N0}).");
                }

                var balanceInfo = await GetAvailableAmountInHandAsync(null, cancellationToken);
                if (allocatedAmount > balanceInfo.AvailableAmountInHand)
                {
                    throw new InvalidOperationException($"The selected deduction (₹{allocatedAmount:N0}) exceeds the available Amount In Hand (₹{balanceInfo.AvailableAmountInHand:N0}).");
                }
            }

            string? eventDatesSerialized = !string.IsNullOrWhiteSpace(request.EventDates) ? request.EventDates.Trim() : null;
            if (request.EventTypeMemberDates != null && request.EventTypeMemberDates.Count > 0)
            {
                eventDatesSerialized = System.Text.Json.JsonSerializer.Serialize(request.EventTypeMemberDates);
            }
            else if (request.EventTypeDates != null && request.EventTypeDates.Count > 0)
            {
                eventDatesSerialized = System.Text.Json.JsonSerializer.Serialize(request.EventTypeDates);
            }

            var cleanDesc = (request.Description ?? string.Empty).Trim();
            if (cleanDesc.Length > 900)
            {
                cleanDesc = cleanDesc.Substring(0, 900);
            }

            var activeCreatorRole = _currentUserService?.Role;
            if (string.IsNullOrWhiteSpace(activeCreatorRole))
            {
                activeCreatorRole = _currentUserService?.IsOrganizerRole == true ? "Organizer" : "Admin";
            }

            var packedDescription = EventMetadataHelper.PackDescription(
                cleanDesc,
                request.UsePreviousEventBalance && appliedAdjustment > 0,
                appliedAdjustment,
                appliedAdjustmentType,
                plannedBudget,
                prevEventName,
                activeCreatorRole);

            var eventItem = new Event
            {
                EventId = Guid.NewGuid(),
                EventName = (request.EventName ?? string.Empty).Trim(),
                EventTypeId = eventType.EventTypeId,
                EventDate = request.EventDate.Date,
                EventDates = eventDatesSerialized,
                CreatedBy = user.UserId,
                CreatedAt = DateTime.UtcNow,
                Description = packedDescription,
                Status = request.Status != 0 ? request.Status : EventStatus.Planned,
                BaseAmount = effectiveBudget,
                UseAvailableBalance = (request.UsePreviousEventBalance && appliedAdjustmentType == "SurplusCredit") || request.UseAvailableBalance,
                AllocatedAmount = allocatedAmount
            };

            foreach (var member in members)
            {
                eventItem.Participants.Add(new EventParticipant
                {
                    Id = Guid.NewGuid(),
                    EventId = eventItem.EventId,
                    UserId = member.MemberId,
                    MemberId = member.MemberId,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.UserId
                });
            }

            var remainingBudgetToCollect = (request.UsePreviousEventBalance && appliedAdjustmentType == "DeficitRecovery")
                ? effectiveBudget
                : (eventItem.UseAvailableBalance ? Math.Max(0m, effectiveBudget - allocatedAmount) : effectiveBudget);

            var memberAmounts = CalculateMemberContributionAmounts(
                eventType,
                eventItem.EventDate,
                remainingBudgetToCollect,
                members,
                request.ContributionOverrides ?? new List<ContributionOverrideDto>());

            var creatorDisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

            Guid? pendingStatusId = null;
            if (_statusRepository != null)
            {
                var pendingStatus = await _statusRepository.GetByNameAsync(CommonConstants.PaymentStatuses.Pending, cancellationToken);
                pendingStatusId = pendingStatus?.StatusId;
            }

            var isFullyFunded = request.UseAvailableBalance && remainingBudgetToCollect == 0m;

            var contributions = members.Select(member => new Contribution
            {
                ContributionId = Guid.NewGuid(),
                EventId = eventItem.EventId,
                UserId = member.MemberId,
                MemberId = member.MemberId,
                Amount = isFullyFunded ? 0m : (memberAmounts.TryGetValue(member.MemberId, out var amount) ? amount : 0m),
                StatusId = isFullyFunded ? null : pendingStatusId,
                PaymentStatus = isFullyFunded ? PaymentStatus.Paid : PaymentStatus.Pending,
                PaymentMode = PaymentMode.None,
                PaymentModeId = null,
                PaymentDate = isFullyFunded ? DateTime.UtcNow : null,
                CashAmount = null,
                UpiAmount = null,
                IsActive = true,
                IsDeleted = false,
                CreatedBy = user.UserId,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (eventItem.Status == EventStatus.Published || eventItem.Status == EventStatus.Planned)
            {
                eventItem.PublishedAt = DateTime.UtcNow;
                eventItem.PublishedBy = user.UserId;
            }

            foreach (var c in contributions)
            {
                eventItem.Contributions.Add(c);
            }

            await _eventRepository.AddAsync(eventItem, cancellationToken);
            await _contributionRepository.AddRangeAsync(contributions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Sync Send-Off Beneficiaries if event is published/planned
            if (eventItem.Status != EventStatus.Draft)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var exitClearanceService = scope.ServiceProvider.GetService<IExitClearanceService>();
                    if (exitClearanceService != null)
                    {
                        await exitClearanceService.SyncSendOffBeneficiariesAsync(eventItem.EventId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync send-off beneficiaries for new event {EventId}", eventItem.EventId);
                }
            }

            // Fetch the created event to ensure it is fully committed
            var createdEvent = await GetByIdAsync(eventItem.EventId, cancellationToken);

            // Send notification emails asynchronously ONLY if event is published (never for Drafts)
            if (eventItem.Status != EventStatus.Draft)
            {
                var participantData = members.Select(m => (
                    MemberId: m.MemberId,
                    Name: (string?)m.Name,
                    Email: (string?)m.Email,
                    ContributionAmount: contributions.FirstOrDefault(c => c.MemberId == m.MemberId)?.Amount ?? 0m
                ));

                var exemptCelebrantIds = (request.ContributionOverrides ?? new List<ContributionOverrideDto>())
                    .Where(x => x.Amount == 0)
                    .Select(x => x.MemberId)
                    .ToHashSet();

                if (request.EventTypeMembers != null)
                {
                    foreach (var kvp in request.EventTypeMembers)
                    {
                        if (kvp.Value != null)
                        {
                            foreach (var mId in kvp.Value)
                            {
                                exemptCelebrantIds.Add(mId);
                            }
                        }
                    }
                }

                DispatchEventNotificationEmails(
                    eventItem.EventId,
                    eventItem.EventName,
                    eventItem.EventDate,
                    eventItem.EventDates,
                    eventItem.Description,
                    eventItem.BaseAmount,
                    eventType.EventTypeId,
                    eventType.EventTypeName,
                    exemptCelebrantIds,
                    participantData);
            }

            return createdEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    private void DispatchEventNotificationEmails(
        Guid eventId,
        string? eventName,
        DateTime eventDate,
        string? eventDates,
        string? eventDescription,
        decimal baseAmount,
        Guid eventTypeId,
        string? eventTypeName,
        HashSet<Guid> exemptCelebrantIds,
        IEnumerable<(Guid MemberId, string? Name, string? Email, decimal ContributionAmount)> participants)
    {
        var rawEventDescription = eventDescription ?? string.Empty;

        // Strip internal metadata tags (contrib, celebrants, eventDates, memberDates)
        eventDescription = System.Text.RegularExpressions.Regex
            .Replace(rawEventDescription, @"<!--(contrib|celebrants|eventDates|memberDates):[\s\S]*?-->", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Trim();

        var particularContributors = participants
            .Where(m => !string.IsNullOrWhiteSpace(m.Email)
                     && !m.Email.Trim().EndsWith("@nomail.com", StringComparison.OrdinalIgnoreCase))
            .Select(m => new
            {
                m.MemberId,
                Name = !string.IsNullOrWhiteSpace(m.Name) ? m.Name.Trim() : "Member",
                Email = m.Email!.Trim(),
                m.ContributionAmount
            })
            .ToList();

        if (particularContributors.Count == 0)
        {
            _logger.LogInformation("No contributors with valid email addresses found for event '{EventName}' ({EventId}). Skipping email dispatch.", eventName, eventId);
            return;
        }

        bool isBirthdayEvent = !string.IsNullOrWhiteSpace(eventTypeName) &&
            eventTypeName.Contains("Birthday", StringComparison.OrdinalIgnoreCase);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<EventService>>();
                var scopedMemberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
                var scopedSettingService = scope.ServiceProvider.GetService<ISystemSettingService>();

                SystemSettingsDto? systemSettings = null;

                if (scopedSettingService != null)
                {
                    try
                    {
                        systemSettings = await scopedSettingService.GetSettingsAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogWarning(ex, CommonLogMessages.Events.SettingsLoadWarning);
                    }
                }

                var orgName = !string.IsNullOrWhiteSpace(systemSettings?.OrgName)
                    ? systemSettings.OrgName.Trim()
                    : "Unit 1A Residents Association";

                var (configuredSubject, configuredDescription) = ResolveConfiguredTemplate(systemSettings, eventTypeId, eventTypeName, "initial");

                var gpayImagePath = ResolveGpayImagePath();
                if (!string.IsNullOrWhiteSpace(gpayImagePath) && File.Exists(gpayImagePath))
                {
                    try
                    {
                        var targetDir = Path.Combine(AppContext.BaseDirectory, CommonConstants.Defaults.WwwRoot);
                        var targetFile = Path.Combine(targetDir, CommonConstants.Defaults.GpayFileName);
                        Directory.CreateDirectory(targetDir);
                        File.Copy(gpayImagePath, targetFile, true);
                    }
                    catch { }
                }

                List<MemberDto> targetCelebrants = new();
                HashSet<Guid> detectedCelebrantIds = new(exemptCelebrantIds);

                // 1. Extract from <!--celebrants:...--> in raw description
                if (!string.IsNullOrWhiteSpace(rawEventDescription) && rawEventDescription.Contains("<!--celebrants:"))
                {
                    try
                    {
                        var startIdx = rawEventDescription.IndexOf("<!--celebrants:") + 15;
                        var endIdx = rawEventDescription.IndexOf("-->", startIdx);
                        if (startIdx >= 15 && endIdx > startIdx)
                        {
                            var jsonStr = rawEventDescription.Substring(startIdx, endIdx - startIdx).Trim();
                            using var doc = JsonDocument.Parse(jsonStr);
                            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var elem in prop.Value.EnumerateArray())
                                        {
                                            if (elem.ValueKind == JsonValueKind.String && Guid.TryParse(elem.GetString(), out var mId))
                                            {
                                                detectedCelebrantIds.Add(mId);
                                            }
                                        }
                                    }
                                    else if (prop.Value.ValueKind == JsonValueKind.String && Guid.TryParse(prop.Value.GetString(), out var mId))
                                    {
                                        detectedCelebrantIds.Add(mId);
                                    }
                                }
                            }
                            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var elem in doc.RootElement.EnumerateArray())
                                {
                                    if (elem.ValueKind == JsonValueKind.String && Guid.TryParse(elem.GetString(), out var mId))
                                    {
                                        detectedCelebrantIds.Add(mId);
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                bool isBirthdayEvent = (!string.IsNullOrWhiteSpace(eventTypeName) && eventTypeName.Contains("Birthday", StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(eventName) && eventName.Contains("Birthday", StringComparison.OrdinalIgnoreCase));

                bool isSendOff = (!string.IsNullOrWhiteSpace(eventTypeName) &&
                    (eventTypeName.Contains("Send", StringComparison.OrdinalIgnoreCase) ||
                     eventTypeName.Contains("Farewell", StringComparison.OrdinalIgnoreCase) ||
                     eventTypeName.Contains("Exit", StringComparison.OrdinalIgnoreCase)))
                    || (!string.IsNullOrWhiteSpace(eventName) &&
                    (eventName.Contains("Send", StringComparison.OrdinalIgnoreCase) ||
                     eventName.Contains("Farewell", StringComparison.OrdinalIgnoreCase) ||
                     eventName.Contains("Exit", StringComparison.OrdinalIgnoreCase)));

                bool isMarriage = (!string.IsNullOrWhiteSpace(eventTypeName) &&
                    (eventTypeName.Contains("Marriage", StringComparison.OrdinalIgnoreCase) ||
                     eventTypeName.Contains("Wedding", StringComparison.OrdinalIgnoreCase)))
                    || (!string.IsNullOrWhiteSpace(eventName) &&
                    (eventName.Contains("Marriage", StringComparison.OrdinalIgnoreCase) ||
                     eventName.Contains("Wedding", StringComparison.OrdinalIgnoreCase)));

                // 2. If Send-Off / Farewell event, also check ExitClearanceService
                if (isSendOff)
                {
                    try
                    {
                        var scopedExitService = scope.ServiceProvider.GetService<IExitClearanceService>();
                        if (scopedExitService != null)
                        {
                            var queue = await scopedExitService.GetQueueAsync(eventId: eventId, cancellationToken: CancellationToken.None);
                            if (queue?.Items != null && queue.Items.Count > 0)
                            {
                                foreach (var item in queue.Items)
                                {
                                    detectedCelebrantIds.Add(item.MemberId);
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 3. For Birthday events, resolve active birthdays in the month
                if (isBirthdayEvent)
                {
                    try
                    {
                        var allMonthCelebrants = await scopedMemberRepo.GetActiveBirthdaysInMonthAsync(eventDate.Month, CancellationToken.None);

                        if (detectedCelebrantIds.Count > 0)
                        {
                            targetCelebrants = allMonthCelebrants.Where(m => detectedCelebrantIds.Contains(m.MemberId)).ToList();
                            if (targetCelebrants.Count == 0)
                            {
                                var allMembers = await scopedMemberRepo.GetAllAsync(CancellationToken.None);
                                targetCelebrants = allMembers.Where(m => detectedCelebrantIds.Contains(m.MemberId)).ToList();
                            }
                        }

                        if (targetCelebrants.Count == 0)
                        {
                            var textToSearch = $"{eventName} {eventDescription}".ToLowerInvariant();
                            targetCelebrants = allMonthCelebrants
                                .Where(m => !string.IsNullOrWhiteSpace(m.Name) &&
                                            (textToSearch.Contains(m.Name.ToLowerInvariant()) ||
                                             textToSearch.Contains(m.MemberId.ToString().ToLowerInvariant())))
                                .ToList();
                        }

                        if (targetCelebrants.Count == 0 && detectedCelebrantIds.Count == 0)
                        {
                            targetCelebrants = allMonthCelebrants;
                        }
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogWarning(ex, "Failed to resolve active birthday celebrants for event email dispatch");
                    }
                }
                else if (detectedCelebrantIds.Count > 0)
                {
                    try
                    {
                        var allMembers = await scopedMemberRepo.GetAllAsync(CancellationToken.None);
                        targetCelebrants = allMembers.Where(m => detectedCelebrantIds.Contains(m.MemberId)).ToList();

                        if (targetCelebrants.Count < detectedCelebrantIds.Count)
                        {
                            var missingIds = detectedCelebrantIds.Where(id => !targetCelebrants.Any(t => t.MemberId == id)).ToList();
                            var direct = await scopedMemberRepo.GetByIdsAsync(missingIds, CancellationToken.None);
                            if (direct != null && direct.Count > 0)
                            {
                                targetCelebrants.AddRange(direct.Select(d => new MemberDto
                                {
                                    MemberId = d.MemberId,
                                    Name = d.Name,
                                    Email = d.Email,
                                    DateOfBirth = d.DateOfBirth
                                }));
                            }
                        }
                    }
                    catch { }
                }

                // 4. Fallback: check zero amount contributions for this event
                if (targetCelebrants.Count == 0 && eventId != Guid.Empty)
                {
                    try
                    {
                        var scopedContribRepo = scope.ServiceProvider.GetService<IContributionRepository>();
                        if (scopedContribRepo != null)
                        {
                            var contribs = await scopedContribRepo.GetByEventIdAsync(eventId, CancellationToken.None);
                            var zeroAmts = contribs.Where(c => c.Amount == 0).Select(c => c.MemberId).ToList();
                            if (zeroAmts.Count > 0)
                            {
                                var zeroMembers = await scopedMemberRepo.GetByIdsAsync(zeroAmts, CancellationToken.None);
                                if (zeroMembers != null && zeroMembers.Count > 0)
                                {
                                    targetCelebrants = zeroMembers.Select(d => new MemberDto
                                    {
                                        MemberId = d.MemberId,
                                        Name = d.Name,
                                        Email = d.Email,
                                        DateOfBirth = d.DateOfBirth
                                    }).ToList();
                                }
                            }
                        }
                    }
                    catch { }
                }

                targetCelebrants = targetCelebrants.OrderBy(c => c.DateOfBirth.Day).ToList();
                var celebrantNames = targetCelebrants.Select(m => m.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

                if (celebrantNames.Count == 0)
                {
                    var nameParts = eventName.Split(new[] { '-', ':', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length > 1)
                    {
                        var candidate = nameParts[1].Trim();
                        if (!string.IsNullOrWhiteSpace(candidate) && !candidate.Equals("event", StringComparison.OrdinalIgnoreCase))
                        {
                            var parsed = candidate.Split(new[] { ',', '&' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList();
                            if (parsed.Count > 0)
                            {
                                celebrantNames = parsed;
                            }
                        }
                    }
                }

                string celebrantsFormatted = celebrantNames.Count > 0 ? string.Join(", ", celebrantNames) : string.Empty;

                var safeCategoryName = !string.IsNullOrWhiteSpace(eventTypeName) ? eventTypeName.Trim() : "Event";
                string celebrantLabel;
                if (isBirthdayEvent)
                {
                    celebrantLabel = celebrantNames.Count > 1 ? "Birthday Celebrants" : "Birthday Celebrant";
                }
                else if (isSendOff)
                {
                    celebrantLabel = celebrantNames.Count > 1 ? "Send-Off Members" : "Send-Off Member";
                }
                else if (isMarriage)
                {
                    celebrantLabel = celebrantNames.Count > 1 ? "Marriage Couple" : "Marriage Celebrant";
                }
                else
                {
                    celebrantLabel = celebrantNames.Count > 1 ? $"{safeCategoryName} Honorees" : $"{safeCategoryName} Honoree";
                }
                eventDescription = eventDescription?.Trim() ?? string.Empty;

                Func<MemberDto, DateTime> getCelebrantBirthDate = (m) =>
                {
                    int birthMonth = m.DateOfBirth.Month >= 1 && m.DateOfBirth.Month <= 12 ? m.DateOfBirth.Month : (eventDate.Month >= 1 && eventDate.Month <= 12 ? eventDate.Month : DateTime.UtcNow.Month);
                    int maxDays = DateTime.DaysInMonth(eventDate.Year, birthMonth);
                    int birthDay = m.DateOfBirth.Day >= 1 && m.DateOfBirth.Day <= maxDays ? m.DateOfBirth.Day : Math.Min(Math.Max(1, m.DateOfBirth.Day), maxDays);
                    return new DateTime(eventDate.Year, birthMonth, birthDay);
                };

                string birthdayDatesSummary = string.Empty;
                string celebrantsAndDatesHtml = string.Empty;

                if (isBirthdayEvent)
                {
                    if (targetCelebrants.Count == 1)
                    {
                        var c = targetCelebrants[0];
                        var bdayDate = getCelebrantBirthDate(c);
                        string bdayDateStr = bdayDate.ToString("MMMM dd, yyyy");
                        birthdayDatesSummary = $"{c.Name} ({bdayDate:MMMM dd})";

                        celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"">Birthday Celebrant:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #7c3aed;"">{c.Name}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Particular Birthday Date:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #c026d3;"">{bdayDateStr}</span>
                </div>";
                    }
                    else if (targetCelebrants.Count > 1)
                    {
                        var summaryItems = targetCelebrants.Select(c =>
                        {
                            var bdayDate = getCelebrantBirthDate(c);
                            return $"{c.Name} ({bdayDate:MMM dd})";
                        });
                        birthdayDatesSummary = string.Join(", ", summaryItems);

                        var tableRows = string.Join("", targetCelebrants.Select(c =>
                        {
                            var bdayDate = getCelebrantBirthDate(c);
                            return $@"
                                <tr style=""border-bottom: 1px dashed rgba(74, 63, 107, 0.1);"">
                                    <td style=""padding: 8px 12px; color: #4a3f6b; font-weight: 700; font-size: 13.5px;"">&#x1F382; {c.Name}</td>
                                    <td align=""right"" style=""padding: 8px 12px; color: #c026d3; font-weight: 700; font-size: 13.5px;"">{bdayDate:MMMM dd, yyyy}</td>
                                </tr>";
                        }));

                        celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"" style=""vertical-align: top; padding-top: 4px;"">Birthday Celebrants &amp; Dates:</span>
                    <div class=""detail-value"" style=""display: block; margin-top: 6px;"">
                        <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""border-collapse: collapse; background-color: #fbfaff; border-radius: 8px; border: 1.5px solid #ece8f8;"">
                            {tableRows}
                        </table>
                    </div>
                </div>";
                    }
                    else if (!string.IsNullOrWhiteSpace(celebrantsFormatted))
                    {
                        celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"">Birthday Celebrants:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #7c3aed;"">{celebrantsFormatted}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Particular Birthday Date:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #c026d3;"">{eventDate:MMMM dd, yyyy}</span>
                </div>";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(celebrantsFormatted))
                {
                    celebrantsAndDatesHtml = $@"
                <div class=""detail-row"">
                    <span class=""detail-label"">{celebrantLabel}:</span>
                    <span class=""detail-value"" style=""font-weight: 700; color: #7c3aed;"">{celebrantsFormatted}</span>
                </div>";
                }

                // Resolve Payment QR config for this event type
                string effectiveReceiverName = orgName;
                string effectiveUpiId = string.Empty;
                string qrMode = "generated";
                string? uploadedQrImage = null;

                if (systemSettings?.PaymentQrConfigs != null)
                {
                    try
                    {
                        var json = systemSettings.PaymentQrConfigs is string s
                            ? s
                            : System.Text.Json.JsonSerializer.Serialize(systemSettings.PaymentQrConfigs);
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var typeKey = (eventTypeName ?? string.Empty).Trim().ToLowerInvariant();
                        var idKey = eventTypeId.ToString().ToLowerInvariant();

                        System.Text.Json.JsonElement matchedElem = default;
                        bool found = false;

                        // 1. Try match by Id or exact event name
                        if (root.TryGetProperty(idKey, out matchedElem) || root.TryGetProperty(typeKey, out matchedElem))
                        {
                            found = true;
                        }

                        // 2. Try loose property matching
                        if (!found)
                        {
                            foreach (var prop in root.EnumerateObject())
                            {
                                var pName = prop.Name.Trim().ToLowerInvariant();
                                if (pName == typeKey || (!string.IsNullOrWhiteSpace(typeKey) && (typeKey.Contains(pName) || pName.Contains(typeKey))))
                                {
                                    matchedElem = prop.Value;
                                    found = true;
                                    break;
                                }
                            }
                        }

                        // 4. Fallback to any configured event type in paymentQrConfigs
                        if (!found)
                        {
                            foreach (var prop in root.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    var u = prop.Value.TryGetProperty("upiId", out var upVal) ? upVal.GetString() : null;
                                    var img = prop.Value.TryGetProperty("qrImage", out var imgVal) ? imgVal.GetString() : null;
                                    if (!string.IsNullOrWhiteSpace(u) || !string.IsNullOrWhiteSpace(img))
                                    {
                                        matchedElem = prop.Value;
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (found && matchedElem.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var upi = matchedElem.TryGetProperty("upiId", out var u) ? u.GetString() : null;
                            var rec = matchedElem.TryGetProperty("receiverName", out var r) ? r.GetString() : null;
                            var mode = matchedElem.TryGetProperty("qrMode", out var m) ? m.GetString() : "generated";
                            var img = matchedElem.TryGetProperty("qrImage", out var qi) ? qi.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(upi)) effectiveUpiId = upi.Trim();
                            if (!string.IsNullOrWhiteSpace(rec)) effectiveReceiverName = rec.Trim();
                            if (!string.IsNullOrWhiteSpace(mode)) qrMode = mode.Trim();
                            uploadedQrImage = img;
                        }
                    }
                    catch { }
                }

                // If not found in systemSettings.PaymentQrConfigs, query EventTypePaymentSettings table from repository
                if (string.IsNullOrWhiteSpace(effectiveUpiId) && string.IsNullOrWhiteSpace(uploadedQrImage))
                {
                    try
                    {
                        var scopedSettingRepo = scope.ServiceProvider.GetService<ISystemSettingRepository>();
                        if (scopedSettingRepo != null)
                        {
                            var allSettings = await scopedSettingRepo.GetAllEventTypePaymentSettingsAsync(CancellationToken.None);
                            if (allSettings != null && allSettings.Count > 0)
                            {
                                var matched = allSettings.FirstOrDefault(s => s.EventTypeId == eventTypeId)
                                    ?? allSettings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.EventTypeName) && s.EventTypeName.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase))
                                    ?? allSettings.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.EventTypeName) && (!string.IsNullOrWhiteSpace(eventTypeName) && (eventTypeName.Contains(s.EventTypeName, StringComparison.OrdinalIgnoreCase) || s.EventTypeName.Contains(eventTypeName, StringComparison.OrdinalIgnoreCase))))
                                    ?? allSettings.FirstOrDefault(s => s.EventTypeName?.Equals("all", StringComparison.OrdinalIgnoreCase) == true)
                                    ?? allSettings.FirstOrDefault(s => s.IsConfigured && (!string.IsNullOrWhiteSpace(s.UpiId) || !string.IsNullOrWhiteSpace(s.QrImageUrl)));

                                if (matched != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(matched.UpiId)) effectiveUpiId = matched.UpiId.Trim();
                                    if (!string.IsNullOrWhiteSpace(matched.ReceiverName)) effectiveReceiverName = matched.ReceiverName.Trim();
                                    if (!string.IsNullOrWhiteSpace(matched.QrMode)) qrMode = matched.QrMode.Trim();
                                    if (!string.IsNullOrWhiteSpace(matched.QrImageUrl)) uploadedQrImage = matched.QrImageUrl.Trim();
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Default fallback if still not configured: ensure scanner is ALWAYS present
                if (string.IsNullOrWhiteSpace(effectiveUpiId) && string.IsNullOrWhiteSpace(uploadedQrImage))
                {
                    var scopedConfig = scope.ServiceProvider.GetService<IConfiguration>();
                    var configUpi = scopedConfig?["PaymentQr:DefaultUpiId"];
                    var configReceiver = scopedConfig?["PaymentQr:DefaultReceiverName"];

                    effectiveUpiId = !string.IsNullOrWhiteSpace(configUpi) ? configUpi.Trim() : "danielrobertanto604@okicici";
                    effectiveReceiverName = !string.IsNullOrWhiteSpace(configReceiver) ? configReceiver.Trim() : (!string.IsNullOrWhiteSpace(orgName) ? orgName : "Unit 1A Residents Association");
                    qrMode = "generated";
                }

                bool isQrConfigured = !string.IsNullOrWhiteSpace(effectiveUpiId) || !string.IsNullOrWhiteSpace(uploadedQrImage);

                foreach (var contributor in particularContributors)
                {
                    try
                    {
                        var memberContributionAmount = contributor.ContributionAmount;
                        var formattedAmount = memberContributionAmount % 1 == 0
                            ? $"₹{memberContributionAmount:N0}"
                            : $"₹{memberContributionAmount:F2}";
                        var formattedDueDate = eventDate.ToString("dd/MM/yyyy");

                        var memberName = !string.IsNullOrWhiteSpace(contributor.Name) ? contributor.Name.Trim() : "Member";
                        var safeEventName = !string.IsNullOrWhiteSpace(eventName) ? eventName.Trim() : "Event";
                        var safeOrgName = !string.IsNullOrWhiteSpace(orgName) ? orgName.Trim() : "Unit 1A Residents Association";

                        string emailSubject = (configuredSubject ?? string.Empty)
                            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{categoryName}", safeCategoryName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventName}", safeEventName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventDescription}", eventDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
                            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
                            .Replace("{orgName}", safeOrgName, StringComparison.OrdinalIgnoreCase);

                        if (string.IsNullOrWhiteSpace(emailSubject))
                        {
                            emailSubject = isBirthdayEvent
                                ? $"Birthday Celebration Contribution - {safeCategoryName}"
                                : $"Contribution Notice - {safeCategoryName}";
                        }

                        string emailHeader = isBirthdayEvent ? "Birthday Celebration" : $"{safeCategoryName} Contribution";

                        string eventDateLabel = "Event Date:";
                        string eventDateValueDisplay = FormatEventDatesForDisplay(eventDates, eventDate, isBirthdayEvent, targetCelebrants);

                        string totalAmountDisplay = isBirthdayEvent && celebrantNames.Count > 1
                            ? $"Rs.{baseAmount:F2} ({celebrantNames.Count} celebrants combined)"
                            : $"Rs.{baseAmount:F2}";

                        var frontendBaseUrl = "http://localhost:5173";
                        var portalContributionUrl = $"{frontendBaseUrl}/contributions";

                        InlineEmailImage? inlineImage = null;
                        string qrCodeCardHtml = string.Empty;

                        if (isQrConfigured)
                        {
                            // Generate Dynamic UPI QR Image for Contributor
                            var upiPaymentUri = $"upi://pay?pa={Uri.EscapeDataString(effectiveUpiId)}&pn={Uri.EscapeDataString(effectiveReceiverName)}&am={memberContributionAmount:F2}&cu=INR&tn={Uri.EscapeDataString($"Contribution for {safeEventName}")}";

                            var qrContentId = $"qr_{Guid.NewGuid():N}";
                            var tempDir = Path.Combine(Path.GetTempPath(), "ContributionManagement_EmailQr");
                            Directory.CreateDirectory(tempDir);

                            if (qrMode.Equals("uploaded", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(uploadedQrImage))
                            {
                                var rawImg = uploadedQrImage.Trim();
                                if (rawImg.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        var commaIdx = rawImg.IndexOf(',');
                                        var base64 = commaIdx >= 0 ? rawImg.Substring(commaIdx + 1) : rawImg;
                                        var imgBytes = Convert.FromBase64String(base64);
                                        var ext = rawImg.Contains("jpeg", StringComparison.OrdinalIgnoreCase) || rawImg.Contains("jpg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                                        var mediaType = ext == "jpg" ? "image/jpeg" : "image/png";
                                        var tempFilePath = Path.Combine(tempDir, $"{qrContentId}.{ext}");
                                        await File.WriteAllBytesAsync(tempFilePath, imgBytes, CancellationToken.None);
                                        inlineImage = new InlineEmailImage(qrContentId, tempFilePath, mediaType);
                                    }
                                    catch { }
                                }
                            }

                            if (inlineImage == null && !string.IsNullOrWhiteSpace(effectiveUpiId))
                            {
                                using var qrGenerator = new QRCodeGenerator();
                                using var qrCodeData = qrGenerator.CreateQrCode(upiPaymentUri, QRCodeGenerator.ECCLevel.Q);
                                using var qrCode = new PngByteQRCode(qrCodeData);
                                var qrBytes = qrCode.GetGraphic(20);
                                var tempFilePath = Path.Combine(tempDir, $"{qrContentId}.png");
                                await File.WriteAllBytesAsync(tempFilePath, qrBytes, CancellationToken.None);
                                inlineImage = new InlineEmailImage(qrContentId, tempFilePath, "image/png");
                            }

                            if (inlineImage != null)
                            {
                                qrCodeCardHtml = $@"
                    <div class=""qr-section"">
                        <div style=""font-size: 13px; font-weight: 700; color: #4a3f6b; margin-bottom: 12px; text-transform: uppercase; letter-spacing: 0.5px;"">
                            Scan QR to Pay via Any UPI App (GPay / PhonePe / Paytm)
                        </div>
                        <div class=""qr-wrapper"">
                            <img src=""cid:{qrContentId}"" alt=""UPI Payment QR Code"" />
                        </div>
                        <div class=""upi-details"">
                            <div><strong>Payee Name:</strong> {effectiveReceiverName}</div>
                            <div style=""margin-top: 4px;""><strong>UPI ID:</strong> {effectiveUpiId}</div>
                            <div style=""margin-top: 6px; font-size: 12px; color: #7c3aed; font-weight: 600;"">
                                Amount is pre-filled automatically when scanned ({formattedAmount})
                            </div>
                        </div>
                        <div style=""text-align: center; margin-top: 14px;"">
                            <a href=""{upiPaymentUri}"" class=""btn-pay"">
                                Pay {formattedAmount} via UPI
                            </a>
                        </div>
                    </div>";
                            }
                        }

                        string detailsCardHtml = $@"
            <div class=""details-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event Name:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{safeEventName}</span>
                </div>
                {celebrantsAndDatesHtml}
                <div class=""detail-row"">
                    <span class=""detail-label"">{eventDateLabel}</span>
                    <span class=""detail-value"" style=""font-weight: 600;"">{eventDateValueDisplay}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Total Amount:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{totalAmountDisplay}</span>
                </div>
                {(string.IsNullOrWhiteSpace(eventDescription) ? string.Empty : $@"<div class=""detail-row"">
                    <span class=""detail-label"">Description:</span>
                    <span class=""detail-value"">{eventDescription}</span>
                </div>")}
                <div class=""detail-row"">
                    <span class=""detail-label"">Contribution Amount:</span>
                    <span class=""detail-value amount-highlight"">{formattedAmount}</span>
                </div>
            </div>";

                        bool qrInCustomDesc = (configuredDescription ?? string.Empty).Contains("{qrCode}", StringComparison.OrdinalIgnoreCase);

                        string qrBlockWithTable = !string.IsNullOrWhiteSpace(qrCodeCardHtml)
                            ? $"{detailsCardHtml}\n{qrCodeCardHtml}"
                            : detailsCardHtml;

                        var interpolated = (configuredDescription ?? string.Empty)
                            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{categoryName}", safeCategoryName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventName}", safeEventName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventDescription}", eventDescription ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
                            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
                            .Replace("{orgName}", safeOrgName, StringComparison.OrdinalIgnoreCase);

                        var formattedText = interpolated
                            .Replace("\r\n", "<br />")
                            .Replace("\n", "<br />");

                        formattedText = System.Text.RegularExpressions.Regex.Replace(formattedText, @"(<br\s*/?>\s*){3,}", "<br /><br />");

                        formattedText = formattedText
                            .Replace("{qrCode}", qrBlockWithTable, StringComparison.OrdinalIgnoreCase)
                            .Replace("{paymentLink}", $@"<div style=""text-align: center; margin: 16px 0;""><a href=""{portalContributionUrl}"" class=""btn-portal"" style=""display: inline-block; background: linear-gradient(135deg, #4a3f6b 0%, #2d2550 100%); color: #ffffff !important; text-decoration: none; font-size: 15px; font-weight: 700; padding: 12px 30px; border-radius: 10px; box-shadow: 0 4px 14px rgba(74, 63, 107, 0.3);"">Go to Contribution Page &amp; Pay</a></div>", StringComparison.OrdinalIgnoreCase);

                        string customMessageHtml = $@"<div style=""font-size: 15px; line-height: 1.7; color: #334155; margin-bottom: 24px;"">{formattedText}</div>";

                        string fallbackCardAndQr = qrInCustomDesc ? string.Empty : $"{detailsCardHtml}\n{qrCodeCardHtml}";

                        var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;700;800&display=swap');
        body {{
            font-family: 'Outfit', 'Inter', 'Segoe UI', sans-serif;
            background-color: #f5f4fb;
            color: #1e1a2e;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 30px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(74, 63, 107, 0.08);
            border: 1px solid rgba(74, 63, 107, 0.08);
        }}
        .header {{
            background: linear-gradient(135deg, #4a3f6b 0%, #2d2550 100%);
            padding: 35px 20px;
            text-align: center;
            color: #ffffff;
            border-bottom: 3px solid #7c3aed;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 800;
            letter-spacing: -0.02em;
        }}
        .content {{
            padding: 30px 25px;
        }}
        .details-card {{
            background-color: #faf9fd;
            border: 1px solid rgba(74, 63, 107, 0.08);
            border-radius: 10px;
            padding: 24px;
            margin-bottom: 25px;
        }}
        .detail-row {{
            margin-bottom: 16px;
            border-bottom: 1px dashed rgba(74, 63, 107, 0.1);
            padding-bottom: 16px;
        }}
        .detail-row:last-child {{
            margin-bottom: 0;
            border-bottom: none;
            padding-bottom: 0;
        }}
        .detail-label {{
            font-weight: 700;
            color: #5b5280;
            display: inline-block;
            width: 160px;
        }}
        .detail-value {{
            color: #1e1a2e;
            display: inline-block;
        }}
        .amount-highlight {{
            font-size: 18px;
            color: #2d2550;
            font-weight: 800;
            background-color: #f0ecf9;
            padding: 4px 10px;
            border-radius: 6px;
            display: inline-block;
        }}
        .qr-section {{
            text-align: center;
            background: #faf5ff;
            border: 1px solid #e9d5ff;
            border-radius: 16px;
            padding: 24px;
            margin-bottom: 25px;
        }}
        .qr-wrapper {{
            display: inline-block;
            padding: 12px;
            background: #ffffff;
            border-radius: 12px;
            border: 2px solid #7c3aed;
            box-shadow: 0 4px 12px rgba(124, 58, 237, 0.12);
            margin-bottom: 12px;
        }}
        .qr-wrapper img {{
            display: block;
            width: 220px;
            height: 220px;
            border-radius: 6px;
            margin: 0 auto;
        }}
        .upi-details {{
            font-size: 13px;
            color: #4b5563;
        }}
        .upi-details strong {{
            color: #312e81;
        }}
        .btn-pay {{
            display: inline-block;
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 12px 28px;
            font-size: 15px;
            font-weight: 700;
            border-radius: 10px;
            text-align: center;
            margin-top: 14px;
            box-shadow: 0 4px 14px rgba(79, 70, 229, 0.35);
        }}
        .btn-portal {{
            display: inline-block;
            background: linear-gradient(135deg, #4a3f6b 0%, #2d2550 100%);
            color: #ffffff !important;
            text-decoration: none;
            font-size: 15px;
            font-weight: 700;
            padding: 14px 34px;
            border-radius: 10px;
            box-shadow: 0 4px 14px rgba(74, 63, 107, 0.3);
            letter-spacing: 0.3px;
        }}
        .footer {{
            background-color: #ffffff;
            padding: 20px;
            text-align: center;
            font-size: 12px;
            color: #5b5280;
            border-top: 1px solid rgba(74, 63, 107, 0.08);
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{emailHeader}</h1>
        </div>
        <div class=""content"">
            {customMessageHtml}
            {fallbackCardAndQr}
        </div>
        <div class=""footer"">
            This is an automated notification from {safeOrgName}.
        </div>
    </div>
</body>
</html>";

                        await scopedEmailService.SendEmailAsync(contributor.Email, emailSubject, emailBody, inlineImage != null ? new[] { inlineImage } : null, CancellationToken.None);
                        scopedLogger.LogInformation(CommonLogMessages.Events.EmailSentSuccess, contributor.Email, safeEventName);
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, CommonLogMessages.Events.EmailSendFailed, contributor.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, CommonLogMessages.Events.EmailTasksFailed, eventId);
            }
        });
    }

    public async Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Events.NotFound);

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                var curUserId = _currentUserService.UserId;
                if (!string.IsNullOrWhiteSpace(curUserId) && Guid.TryParse(curUserId, out var curUserGuid))
                {
                    bool isUserAdmin = _currentUserService.HasRole(CommonRoles.Admin);
                    bool isAllowed = eventItem.CreatedBy == curUserGuid &&
                        (!isUserAdmin || (eventItem.Description != null && eventItem.Description.Contains("CREATOR_ROLE: Organizer")));

                    if (!isAllowed)
                    {
                        throw new UnauthorizedAccessException("Organizers can only edit events they created.");
                    }
                }
            }

            // Disallow editing if any payments have been recorded for this event
            var hasPaidContributions = await _eventRepository.HasPaidContributionsAsync(eventId, cancellationToken)
                || eventItem.Contributions.Any(c =>
                    !c.IsDeleted && (
                        c.PaymentDate != null ||
                        c.PaymentStatus == PaymentStatus.Paid ||
                        (c.StatusItem != null && (
                            c.StatusItem.StatusName.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase))) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    ));

            if (hasPaidContributions)
            {
                throw new InvalidOperationException(CommonMessages.Events.CannotEditPaidEvent);
            }

            var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            // Monthly Duplicate Event Prevention:
            var allTypesToCheck = new HashSet<Guid>();
            if (request.EventTypeId != Guid.Empty) allTypesToCheck.Add(request.EventTypeId);
            if (request.EventTypeIds != null && request.EventTypeIds.Count > 0)
            {
                foreach (var tid in request.EventTypeIds)
                {
                    if (tid != Guid.Empty) allTypesToCheck.Add(tid);
                }
            }

            var allDatesToCheck = new List<DateTime> { request.EventDate.Date };
            if (request.EventTypeDates != null)
            {
                foreach (var dVal in request.EventTypeDates.Values)
                {
                    if (DateTime.TryParse(dVal, out var parsedDate))
                    {
                        allDatesToCheck.Add(parsedDate.Date);
                    }
                }
            }
            if (request.EventTypeMemberDates != null)
            {
                foreach (var innerDict in request.EventTypeMemberDates.Values)
                {
                    foreach (var dVal in innerDict.Values)
                    {
                        if (DateTime.TryParse(dVal, out var parsedDate))
                        {
                            allDatesToCheck.Add(parsedDate.Date);
                        }
                    }
                }
            }

            var distinctMonthYears = allDatesToCheck
                .Select(d => (Month: d.Month, Year: d.Year))
                .Distinct()
                .ToList();

            // Validate Monthly Event Restrictions:
            // Rule A: Birthday is allowed only once per calendar month
            // Rule B: Non-Birthday prevents duplicate celebrants for the same event type in the same calendar month
            await ValidateMonthlyEventRulesAsync(request, eventType, eventId, cancellationToken);

            if (request.BaseAmount <= 0)
            {
                throw new InvalidOperationException("Event cannot be saved with ₹0. Please configure budget calculation rates or enter a valid contribution amount.");
            }

            eventItem.EventName = (request.EventName ?? string.Empty).Trim();
            eventItem.EventTypeId = eventType.EventTypeId;
            eventItem.EventDate = request.EventDate.Date;
            if (request.EventTypeMemberDates != null && request.EventTypeMemberDates.Count > 0)
            {
                eventItem.EventDates = System.Text.Json.JsonSerializer.Serialize(request.EventTypeMemberDates);
            }
            else if (request.EventTypeDates != null && request.EventTypeDates.Count > 0)
            {
                eventItem.EventDates = System.Text.Json.JsonSerializer.Serialize(request.EventTypeDates);
            }
            else
            {
                eventItem.EventDates = !string.IsNullOrWhiteSpace(request.EventDates) ? request.EventDates.Trim() : null;
            }
            var cleanDesc = (request.Description ?? string.Empty).Trim();
            if (cleanDesc.Length > 900)
            {
                cleanDesc = cleanDesc.Substring(0, 900);
            }

            decimal plannedBudget = RoundingHelper.RoundToWhole(request.BaseAmount);
            decimal effectiveBudget = plannedBudget;
            decimal allocatedAmount = 0m;
            decimal appliedAdjustment = 0m;
            string appliedAdjustmentType = "None";
            string? prevEventName = null;

            if (request.UsePreviousEventBalance)
            {
                var balanceInfo = await GetAvailableAmountInHandAsync(eventId, cancellationToken);
                if (balanceInfo.EligibleAdjustmentAmount > 0)
                {
                    appliedAdjustment = balanceInfo.EligibleAdjustmentAmount;
                    appliedAdjustmentType = balanceInfo.AdjustmentType;
                    prevEventName = balanceInfo.PreviousEventName;

                    if (appliedAdjustmentType == "DeficitRecovery")
                    {
                        effectiveBudget = plannedBudget + appliedAdjustment;
                        allocatedAmount = 0m;
                    }
                    else if (appliedAdjustmentType == "SurplusCredit")
                    {
                        appliedAdjustment = Math.Min(appliedAdjustment, plannedBudget);
                        allocatedAmount = appliedAdjustment;
                    }
                }
            }
            else if (request.UseAvailableBalance)
            {
                allocatedAmount = request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0
                    ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value)
                    : plannedBudget;

                if (allocatedAmount > plannedBudget)
                {
                    throw new InvalidOperationException($"The deducted balance amount (₹{allocatedAmount:N0}) cannot exceed the planned budget (₹{plannedBudget:N0}).");
                }
            }

            var (_, _, _, _, _, _, existingCreatorRole) = EventMetadataHelper.UnpackDescriptionWithRole(eventItem.Description);
            var updatedCreatorRole = existingCreatorRole ?? (_currentUserService?.IsOrganizerRole == true ? "Organizer" : "Admin");

            var packedDescription = EventMetadataHelper.PackDescription(
                cleanDesc,
                request.UsePreviousEventBalance && appliedAdjustment > 0,
                appliedAdjustment,
                appliedAdjustmentType,
                plannedBudget,
                prevEventName,
                updatedCreatorRole);

            eventItem.Description = packedDescription;
            if (request.Status != 0)
            {
                eventItem.Status = request.Status;
            }

            eventItem.BaseAmount = effectiveBudget;
            eventItem.UseAvailableBalance = (request.UsePreviousEventBalance && appliedAdjustmentType == "SurplusCredit") || request.UseAvailableBalance;
            eventItem.AllocatedAmount = allocatedAmount;
            eventItem.ModifiedOn = DateTime.UtcNow;

            var newParticipantIds = (request.ParticipantIds ?? new List<Guid>()).Distinct().ToList();
            if (newParticipantIds.Count == 0)
            {
                throw new InvalidOperationException(CommonMessages.Events.AtLeastOneParticipantRequired);
            }

            var members = await _memberRepository.GetByIdsAsync(newParticipantIds, cancellationToken);
            if (members.Count != newParticipantIds.Count)
            {
                throw new InvalidOperationException(CommonMessages.Events.ParticipantsNotFound);
            }

            var remainingBudgetToCollect = (request.UsePreviousEventBalance && appliedAdjustmentType == "DeficitRecovery")
                ? effectiveBudget
                : (eventItem.UseAvailableBalance ? Math.Max(0m, effectiveBudget - allocatedAmount) : effectiveBudget);

            var memberAmounts = CalculateMemberContributionAmounts(
                eventType,
                eventItem.EventDate,
                remainingBudgetToCollect,
                members,
                request.ContributionOverrides ?? new List<ContributionOverrideDto>());

            // 1. Synchronize Event Participants
            var allExistingParticipants = eventItem.Participants.ToList();
            var participantLookup = allExistingParticipants
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var participantIdsToAdd = new List<Guid>();

            // Deactivate participants not present in newParticipantIds
            foreach (var p in allExistingParticipants)
            {
                if (!newParticipantIds.Contains(p.UserId))
                {
                    if (!p.IsDeleted)
                    {
                        p.IsDeleted = true;
                        p.ModifiedOn = DateTime.UtcNow;
                    }
                }
            }

            // Add or Reactivate requested participants
            var newParticipantsToAdd = new List<EventParticipant>();
            foreach (var memberId in newParticipantIds)
            {
                if (participantLookup.TryGetValue(memberId, out var existingList) && existingList.Count > 0)
                {
                    var primary = existingList.First();
                    if (primary.IsDeleted)
                    {
                        primary.IsDeleted = false;
                        primary.IsActive = true;
                        primary.ModifiedOn = DateTime.UtcNow;
                        participantIdsToAdd.Add(memberId);
                    }
                    for (int i = 1; i < existingList.Count; i++)
                    {
                        if (!existingList[i].IsDeleted)
                        {
                            existingList[i].IsDeleted = true;
                            existingList[i].ModifiedOn = DateTime.UtcNow;
                        }
                    }
                }
                else
                {
                    var newParticipant = new EventParticipant
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventItem.EventId,
                        UserId = memberId,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    eventItem.Participants.Add(newParticipant);
                    newParticipantsToAdd.Add(newParticipant);
                    participantIdsToAdd.Add(memberId);
                }
            }

            if (newParticipantsToAdd.Count > 0)
            {
                await _eventRepository.AddParticipantsAsync(newParticipantsToAdd, cancellationToken);
            }

            // 2. Synchronize Contributions
            var allExistingContributions = eventItem.Contributions.ToList();
            var contribLookup = allExistingContributions
                .GroupBy(c => c.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Deactivate contributions for removed participants
            foreach (var c in allExistingContributions)
            {
                if (!newParticipantIds.Contains(c.UserId))
                {
                    if (!c.IsDeleted)
                    {
                        c.IsDeleted = true;
                        c.ModifiedOn = DateTime.UtcNow;
                    }
                }
            }

            Guid? pendingStatusId = null;
            if (_statusRepository != null)
            {
                var pendingStatus = await _statusRepository.GetByNameAsync(CommonConstants.PaymentStatuses.Pending, cancellationToken);
                pendingStatusId = pendingStatus?.StatusId;
            }

            // Update or Add contributions for requested participants
            var newContributionsToAdd = new List<Contribution>();
            foreach (var memberId in newParticipantIds)
            {
                if (!memberAmounts.TryGetValue(memberId, out var amount))
                {
                    continue;
                }

                if (contribLookup.TryGetValue(memberId, out var existingList) && existingList.Count > 0)
                {
                    var primary = existingList.First();
                    primary.Amount = amount;
                    primary.IsDeleted = false;
                    primary.IsActive = true;
                    primary.ModifiedOn = DateTime.UtcNow;

                    for (int i = 1; i < existingList.Count; i++)
                    {
                        if (!existingList[i].IsDeleted)
                        {
                            existingList[i].IsDeleted = true;
                            existingList[i].ModifiedOn = DateTime.UtcNow;
                        }
                    }
                }
                else
                {
                    var newContribution = new Contribution
                    {
                        ContributionId = Guid.NewGuid(),
                        EventId = eventItem.EventId,
                        UserId = memberId,
                        Amount = amount,
                        StatusId = pendingStatusId,
                        PaymentStatus = PaymentStatus.Pending,
                        PaymentMode = PaymentMode.None,
                        PaymentModeId = null,
                        PaymentDate = null,
                        CashAmount = null,
                        UpiAmount = null,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    eventItem.Contributions.Add(newContribution);
                    newContributionsToAdd.Add(newContribution);
                }
            }

            if (newContributionsToAdd.Count > 0)
            {
                await _contributionRepository.AddRangeAsync(newContributionsToAdd, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Sync Send-Off Beneficiaries if event is published/planned
            if (eventItem.Status != EventStatus.Draft)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var exitClearanceService = scope.ServiceProvider.GetService<IExitClearanceService>();
                    if (exitClearanceService != null)
                    {
                        await exitClearanceService.SyncSendOffBeneficiariesAsync(eventItem.EventId, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync send-off beneficiaries for updated event {EventId}", eventItem.EventId);
                }
            }

            // If new participants were added, dispatch notification emails ONLY if event is published (never for Drafts)
            if (participantIdsToAdd.Count > 0 && eventItem.Status != EventStatus.Draft)
            {
                var newMembers = members.Where(m => participantIdsToAdd.Contains(m.MemberId)).ToList();
                var newParticipantData = newMembers.Select(m => (
                    MemberId: m.MemberId,
                    Name: (string?)m.Name,
                    Email: (string?)m.Email,
                    ContributionAmount: memberAmounts.TryGetValue(m.MemberId, out var amt) ? amt : 0m
                ));
                var exemptCelebrantIds = (request.ContributionOverrides ?? new List<ContributionOverrideDto>())
                    .Where(x => x.Amount == 0)
                    .Select(x => x.MemberId)
                    .ToHashSet();

                if (request.EventTypeMembers != null)
                {
                    foreach (var kvp in request.EventTypeMembers)
                    {
                        if (kvp.Value != null)
                        {
                            foreach (var mId in kvp.Value)
                            {
                                exemptCelebrantIds.Add(mId);
                            }
                        }
                    }
                }

                DispatchEventNotificationEmails(
                    eventItem.EventId,
                    eventItem.EventName,
                    eventItem.EventDate,
                    eventItem.EventDates,
                    eventItem.Description,
                    eventItem.BaseAmount,
                    eventType.EventTypeId,
                    eventType.EventTypeName,
                    exemptCelebrantIds,
                    newParticipantData);
            }

            return await GetByIdAsync(eventItem.EventId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Events.NotFound);

            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                var curUserId = _currentUserService.UserId;
                if (!string.IsNullOrWhiteSpace(curUserId) && Guid.TryParse(curUserId, out var curUserGuid))
                {
                    bool isUserAdmin = _currentUserService.HasRole(CommonRoles.Admin);
                    bool isAllowed = eventItem.CreatedBy == curUserGuid &&
                        (!isUserAdmin || (eventItem.Description != null && eventItem.Description.Contains("CREATOR_ROLE: Organizer")));

                    if (!isAllowed)
                    {
                        throw new UnauthorizedAccessException("Organizers can only delete events they created.");
                    }
                }
            }

            var hasPaidContributions = await _eventRepository.HasPaidContributionsAsync(eventId, cancellationToken)
                || (eventItem.Contributions != null && eventItem.Contributions.Any(c =>
                    !c.IsDeleted && (
                        c.PaymentDate != null ||
                        c.PaymentStatus == PaymentStatus.Paid ||
                        (c.StatusItem != null && (
                            c.StatusItem.StatusName.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                            c.StatusItem.StatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase))) ||
                        (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                        (c.UpiAmount.HasValue && c.UpiAmount.Value > 0)
                    )));

            if (hasPaidContributions)
            {
                throw new InvalidOperationException(CommonMessages.Events.CannotDeletePaidEvent);
            }

            eventItem.IsDeleted = true;
            eventItem.ModifiedOn = DateTime.UtcNow;

            if (eventItem.Contributions != null)
            {
                foreach (var contribution in eventItem.Contributions)
                {
                    if (!contribution.IsDeleted)
                    {
                        contribution.IsDeleted = true;
                        contribution.ModifiedOn = DateTime.UtcNow;
                    }
                }
            }

            // Also delete any gallery photos uploaded for this event
            var galleryPhotos = await _galleryRepository.GetAllAsync(eventItem.EventName, null, cancellationToken);
            if (galleryPhotos != null && galleryPhotos.Count > 0)
            {
                var photoIds = galleryPhotos.Select(p => p.PhotoId).ToList();
                foreach (var pid in photoIds)
                {
                    var photoEntity = await _galleryRepository.GetByIdAsync(pid, cancellationToken);
                    if (photoEntity != null)
                    {
                        photoEntity.IsDeleted = true;
                        photoEntity.ModifiedOn = DateTime.UtcNow;
                    }
                }
            }

            // Handle exit clearances on event deletion
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var exitClearanceService = scope.ServiceProvider.GetService<IExitClearanceService>();
                if (exitClearanceService != null)
                {
                    await exitClearanceService.HandleEventDeletedAsync(eventId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to handle exit clearances on delete for event {EventId}", eventId);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }

    public async Task<EventDetailsDto> PublishAsync(Guid eventId, Guid publishingUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventItem = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Events.NotFound);

            if (eventItem.IsDeleted)
            {
                throw new InvalidOperationException("Cannot publish a deleted event.");
            }

            // Verify Organizer permission: Organizers can only publish events they created
            if (_currentUserService != null && _currentUserService.IsOrganizerRole)
            {
                var curUserId = _currentUserService.UserId;
                if (!string.IsNullOrWhiteSpace(curUserId) && Guid.TryParse(curUserId, out var curUserGuid))
                {
                    bool isUserAdmin = _currentUserService.HasRole(CommonRoles.Admin);
                    bool isAllowed = eventItem.CreatedBy == curUserGuid &&
                        (!isUserAdmin || (eventItem.Description != null && eventItem.Description.Contains("CREATOR_ROLE: Organizer")));

                    if (!isAllowed)
                    {
                        throw new UnauthorizedAccessException("Organizers can only publish events they created.");
                    }
                }
            }

            // Idempotent: If already published/planned/active/completed, return existing details without duplicated notifications
            if (eventItem.Status == EventStatus.Published || eventItem.Status == EventStatus.Active || eventItem.Status == EventStatus.Completed)
            {
                return await GetByIdAsync(eventItem.EventId, cancellationToken);
            }

            var eventType = await _eventTypeRepository.GetByIdAsync(eventItem.EventTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            var activeParticipants = eventItem.Participants.Where(p => !p.IsDeleted).ToList();
            if (activeParticipants.Count == 0)
            {
                throw new InvalidOperationException(CommonMessages.Events.AtLeastOneParticipantRequired);
            }

            if (eventItem.BaseAmount <= 0)
            {
                throw new InvalidOperationException("Event cannot be published with ₹0. Please configure budget calculation rates or enter a valid contribution amount.");
            }

            // Update status and audit
            eventItem.Status = EventStatus.Published;
            eventItem.PublishedAt = DateTime.UtcNow;
            eventItem.PublishedBy = publishingUserId;
            eventItem.ModifiedBy = publishingUserId;
            eventItem.ModifiedOn = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var exitClearanceService = scope.ServiceProvider.GetService<IExitClearanceService>();
                if (exitClearanceService != null)
                {
                    await exitClearanceService.SyncSendOffBeneficiariesAsync(eventItem.EventId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync send-off beneficiaries for published event {EventId}", eventItem.EventId);
            }

            // Send notification emails to all active participant contributors
            var participantMemberIds = activeParticipants.Select(p => p.UserId).Distinct().ToList();
            var members = await _memberRepository.GetByIdsAsync(participantMemberIds, cancellationToken);
            var activeContributions = eventItem.Contributions.Where(c => !c.IsDeleted).ToList();

            var participantData = members.Select(m => (
                MemberId: m.MemberId,
                Name: (string?)m.Name,
                Email: (string?)m.Email,
                ContributionAmount: activeContributions.FirstOrDefault(c => c.UserId == m.MemberId)?.Amount ?? 0m
            ));

            var exemptCelebrantIds = activeContributions
                .Where(c => c.Amount == 0)
                .Select(c => c.UserId)
                .ToHashSet();

            DispatchEventNotificationEmails(
                eventItem.EventId,
                eventItem.EventName,
                eventItem.EventDate,
                eventItem.EventDates,
                eventItem.Description,
                eventItem.BaseAmount,
                eventType.EventTypeId,
                eventType.EventTypeName,
                exemptCelebrantIds,
                participantData);

            return await GetByIdAsync(eventItem.EventId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(PublishAsync));
            throw;
        }
    }
    /// <summary>
    /// Computes contribution amounts for members based on dynamic event type tenure rules and custom overrides.
    /// </summary>
    private static Dictionary<Guid, decimal> CalculateMemberContributionAmounts(
        EventType eventType,
        DateTime eventDate,
        decimal baseAmount,
        IReadOnlyCollection<Member> members,
        IReadOnlyCollection<ContributionOverrideDto> overrides)
    {
        var details = CalculateDetailedMemberContributions(eventType, eventDate, baseAmount, members, overrides);
        return details.ToDictionary(x => x.MemberId, x => x.Amount);
    }

    /// <summary>
    /// Core calculation engine returning detailed breakdown per member.
    /// Used identically for both pre-submission preview and final event creation.
    /// </summary>
    private static List<MemberContributionCalculationResult> CalculateDetailedMemberContributions(
        EventType eventType,
        DateTime eventDate,
        decimal baseAmount,
        IReadOnlyCollection<Member> members,
        IReadOnlyCollection<ContributionOverrideDto>? overrides)
    {
        var overrideLookup = (overrides ?? Array.Empty<ContributionOverrideDto>())
            .GroupBy(x => x.MemberId)
            .ToDictionary(x => x.Key, x => RoundingHelper.RoundToWhole(x.Last().Amount));

        bool hasTenureRule = eventType.HasTenureRule;
        decimal thresholdYears = eventType.TenureThresholdYears > 0 ? eventType.TenureThresholdYears : 1.0m;
        decimal newEntrantRatio = (eventType.NewEntrantSharePercentage > 0 ? eventType.NewEntrantSharePercentage : 50.0m) / 100.0m;
        decimal standardRatio = (eventType.StandardSharePercentage > 0 ? eventType.StandardSharePercentage : 100.0m) / 100.0m;

        decimal totalBaseDecimal = RoundingHelper.RoundToWhole(baseAmount);
        decimal overrideSum = overrideLookup.Values.Sum();
        decimal splitPool = Math.Max(0m, totalBaseDecimal - overrideSum);

        var nonOverrideMembers = members.Where(m => !overrideLookup.ContainsKey(m.MemberId)).ToList();
        var results = new List<MemberContributionCalculationResult>();

        decimal baseStandardAmount = members.Count > 0 ? RoundingHelper.RoundToWhole(totalBaseDecimal / members.Count) : 0m;

        foreach (var member in members)
        {
            if (overrideLookup.TryGetValue(member.MemberId, out var customAmount))
            {
                double tenureDays = (eventDate.Date - member.JoiningDate.Date).TotalDays;
                decimal tenureYears = Math.Max(0m, Math.Round((decimal)(tenureDays / 365.25), 1, MidpointRounding.AwayFromZero));

                results.Add(new MemberContributionCalculationResult
                {
                    MemberId = member.MemberId,
                    Amount = customAmount,
                    CompletedYears = tenureYears,
                    SharePercentage = 100m,
                    StandardAmount = baseStandardAmount
                });
            }
        }

        if (nonOverrideMembers.Count > 0)
        {
            if (hasTenureRule)
            {
                var dynamicTiers = new List<(decimal Years, decimal ShareRatio)>();
                if (!string.IsNullOrWhiteSpace(eventType.RuleDescription))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(eventType.RuleDescription);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in doc.RootElement.EnumerateArray())
                            {
                                decimal y = 0;
                                decimal s = 0;
                                if (item.TryGetProperty("years", out var yp)) y = yp.GetDecimal();
                                else if (item.TryGetProperty("y", out var yps)) y = yps.GetDecimal();

                                if (item.TryGetProperty("share", out var sp)) s = sp.GetDecimal();
                                else if (item.TryGetProperty("s", out var sps)) s = sps.GetDecimal();

                                if (y > 0 && s > 0)
                                {
                                    dynamicTiers.Add((y, s / 100.0m));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to legacy single threshold rule if RuleDescription is not JSON
                    }
                }

                if (dynamicTiers.Count == 0)
                {
                    dynamicTiers.Add((thresholdYears, newEntrantRatio));
                }

                dynamicTiers = dynamicTiers.OrderBy(t => t.Years).ToList();

                var memberRatios = new Dictionary<Guid, (decimal Ratio, decimal Years)>();
                foreach (var member in nonOverrideMembers)
                {
                    double tenureDays = (eventDate.Date - member.JoiningDate.Date).TotalDays;
                    decimal tenureYears = Math.Max(0m, Math.Round((decimal)(tenureDays / 365.25), 1, MidpointRounding.AwayFromZero));

                    decimal assignedRatio = standardRatio;
                    foreach (var tier in dynamicTiers)
                    {
                        if (tenureYears < tier.Years)
                        {
                            assignedRatio = tier.ShareRatio;
                            break;
                        }
                    }

                    memberRatios[member.MemberId] = (assignedRatio, tenureYears);
                }

                decimal divisor = memberRatios.Values.Sum(x => x.Ratio);
                decimal standardShare = divisor > 0 ? (splitPool / divisor) : 0m;
                baseStandardAmount = RoundingHelper.RoundToWhole(standardShare);

                var rawShares = nonOverrideMembers.Select(m => standardShare * memberRatios[m.MemberId].Ratio).ToList();
                var distributedShares = RoundingHelper.DistributeRoundedTotal(splitPool, rawShares);

                for (int i = 0; i < nonOverrideMembers.Count; i++)
                {
                    var member = nonOverrideMembers[i];
                    var (shareRatio, tenureYears) = memberRatios[member.MemberId];
                    results.Add(new MemberContributionCalculationResult
                    {
                        MemberId = member.MemberId,
                        Amount = distributedShares[i],
                        CompletedYears = tenureYears,
                        SharePercentage = Math.Round(shareRatio * 100m, 1),
                        StandardAmount = baseStandardAmount
                    });
                }
            }
            else
            {
                var distributedShares = RoundingHelper.DistributeEqualSplit(splitPool, nonOverrideMembers.Count);
                baseStandardAmount = distributedShares.Count > 0 ? distributedShares.Max() : 0m;

                for (int i = 0; i < nonOverrideMembers.Count; i++)
                {
                    var member = nonOverrideMembers[i];
                    double tenureDays = (eventDate.Date - member.JoiningDate.Date).TotalDays;
                    decimal tenureYears = Math.Max(0m, Math.Round((decimal)(tenureDays / 365.25), 1, MidpointRounding.AwayFromZero));

                    results.Add(new MemberContributionCalculationResult
                    {
                        MemberId = member.MemberId,
                        Amount = distributedShares[i],
                        CompletedYears = tenureYears,
                        SharePercentage = 100m,
                        StandardAmount = baseStandardAmount
                    });
                }
            }
        }

        return results;
    }

    public async Task<EventContributionPreviewResponseDto> CalculateContributionPreviewAsync(
        EventContributionPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var targetTypeId = request.EventTypeId != Guid.Empty
                ? request.EventTypeId
                : (request.EventTypeIds != null && request.EventTypeIds.Count > 0 ? request.EventTypeIds[0] : Guid.Empty);

            var eventType = targetTypeId != Guid.Empty
                ? await _eventTypeRepository.GetByIdAsync(targetTypeId, cancellationToken)
                : null;

            if (eventType == null)
            {
                throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);
            }

            var participantIds = (request.ParticipantIds ?? new List<Guid>()).Distinct().ToList();
            if (participantIds.Count == 0)
            {
                return new EventContributionPreviewResponseDto
                {
                    TotalMembers = 0,
                    EligibleContributors = 0,
                    PlannedBudget = request.BaseAmount,
                    ExpectedCollection = 0m,
                    StandardContributionAmount = 0m,
                    Members = Array.Empty<EventContributionPreviewMemberDto>()
                };
            }

            var members = await _memberRepository.GetByIdsAsync(participantIds, cancellationToken);
            if (members.Count == 0)
            {
                return new EventContributionPreviewResponseDto
                {
                    TotalMembers = 0,
                    EligibleContributors = 0,
                    PlannedBudget = request.BaseAmount,
                    ExpectedCollection = 0m,
                    StandardContributionAmount = 0m,
                    Members = Array.Empty<EventContributionPreviewMemberDto>()
                };
            }

            decimal plannedBudget = RoundingHelper.RoundToWhole(request.BaseAmount);
            var memberLookup = members.ToDictionary(m => m.MemberId);

            decimal effectiveBudget = plannedBudget;
            decimal eligibleAdj = 0m;
            string adjType = "None";

            var balanceInfo = await GetAvailableAmountInHandAsync(null, cancellationToken);
            if (request.UsePreviousEventBalance && balanceInfo.EligibleAdjustmentAmount > 0)
            {
                adjType = balanceInfo.AdjustmentType;
                eligibleAdj = balanceInfo.EligibleAdjustmentAmount;

                if (adjType == "DeficitRecovery")
                {
                    effectiveBudget = plannedBudget + eligibleAdj;
                }
                else if (adjType == "SurplusCredit")
                {
                    eligibleAdj = Math.Min(eligibleAdj, plannedBudget);
                }
            }

            decimal allocatedAmount = 0m;
            decimal remainingBudgetToCollect;

            if (request.UsePreviousEventBalance && adjType == "DeficitRecovery")
            {
                // Deficit recovery increases collection requirement to recover previous overspend
                remainingBudgetToCollect = effectiveBudget;
            }
            else if (request.UsePreviousEventBalance && adjType == "SurplusCredit")
            {
                // Surplus credit acts as allocated amount deduction
                allocatedAmount = eligibleAdj;
                remainingBudgetToCollect = Math.Max(0m, plannedBudget - allocatedAmount);
            }
            else
            {
                // Normal available balance deduction logic
                allocatedAmount = request.UseAvailableBalance
                    ? (request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0 ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value) : plannedBudget)
                    : 0m;
                remainingBudgetToCollect = request.UseAvailableBalance
                    ? Math.Max(0m, plannedBudget - allocatedAmount)
                    : plannedBudget;
            }

            var detailedCalculations = CalculateDetailedMemberContributions(
                eventType,
                request.EventDate.Date,
                remainingBudgetToCollect,
                members,
                request.ContributionOverrides);

            var previewMembers = new List<EventContributionPreviewMemberDto>();
            foreach (var calc in detailedCalculations)
            {
                if (memberLookup.TryGetValue(calc.MemberId, out var mem))
                {
                    previewMembers.Add(new EventContributionPreviewMemberDto
                    {
                        UserId = mem.MemberId,
                        MemberId = mem.MemberId,
                        MemberName = mem.Name ?? string.Empty,
                        WorkType = !string.IsNullOrWhiteSpace(mem.WorkType) ? mem.WorkType : "Office",
                        CompletedYears = calc.CompletedYears,
                        SharePercentage = calc.SharePercentage,
                        StandardAmount = calc.StandardAmount,
                        ContributionAmount = calc.Amount
                    });
                }
            }

            // Keep same order as participantIds requested
            var orderedPreview = (request.ParticipantIds != null ? request.ParticipantIds : participantIds)
                .Select(id => previewMembers.FirstOrDefault(m => m.MemberId == id))
                .Where(m => m != null)
                .Select(m => m!)
                .ToList();

            decimal totalCalculated = orderedPreview.Count > 0 ? orderedPreview.Sum(m => m.ContributionAmount) : 0m;
            decimal standardAmt = orderedPreview.Count > 0 ? orderedPreview.Max(m => m.StandardAmount) : 0m;

            return new EventContributionPreviewResponseDto
            {
                TotalMembers = members != null ? members.Count : 0,
                EligibleContributors = orderedPreview.Count,
                PlannedBudget = plannedBudget,
                ExpectedCollection = RoundingHelper.RoundToWhole(totalCalculated),
                StandardContributionAmount = RoundingHelper.RoundToWhole(standardAmt),
                UsePreviousEventBalance = request.UsePreviousEventBalance,
                EligibleAdjustmentAmount = eligibleAdj,
                AdjustmentType = adjType,
                EffectiveBudget = effectiveBudget,
                Members = orderedPreview
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CalculateContributionPreviewAsync));
            throw;
        }
    }

    private static string? ResolveGpayImagePath()
    {
        var candidatePaths = new[]
        {
            @"d:\ContributionManagement\backend\ContributionManagement\TeamContributionManagementSystem.API\wwwroot\gpay.png",
            @"d:\ContributionManagement\frontend\ContributionManagementUI\src\assets\payment_qr.png",
            @"C:\Users\clean_accont\.gemini\antigravity-ide\brain\38024a44-87e5-40a2-9b70-3a9c518582c7\.user_uploaded\media_1790009320912.png",
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "gpay.png"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "gpay.png"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "gpay.png"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "gpay.png")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "gpay.png")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.API", "wwwroot", "gpay.png")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "TeamContributionManagementSystem.API", "wwwroot", "gpay.png"))
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static (string Subject, string Description) ResolveConfiguredTemplate(
        SystemSettingsDto? settings,
        Guid eventTypeId,
        string eventTypeName,
        string templateType = "initial")
    {
        string defaultSubject = "Contribution Notice - {categoryName}";
        string defaultDesc = "Dear {memberName},\n\nThis is a notification regarding your contribution for {categoryName} of {amount}, due by {dueDate}.\n\n{eventDescription}\n\nPlease scan the dynamic UPI QR code below or log in to the portal to complete your contribution payment.\n\n{qrCode}\n\n{paymentLink}\n\nThank you,\n{orgName}";

        string fallbackSubject = !string.IsNullOrWhiteSpace(settings?.EmailSubject) ? settings.EmailSubject.Trim() : defaultSubject;
        string fallbackDesc = !string.IsNullOrWhiteSpace(settings?.EmailDescription) ? settings.EmailDescription.Trim() : defaultDesc;

        if (settings == null) return (fallbackSubject, fallbackDesc);

        JsonElement root = default;
        bool hasJson = false;

        if (settings.CategoryTemplates is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            root = element;
            hasJson = true;
        }
        else if (settings.CategoryTemplates is string jsonStr && !string.IsNullOrWhiteSpace(jsonStr))
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonStr);
                root = doc.RootElement.Clone();
                hasJson = true;
            }
            catch { }
        }
        else if (settings.CategoryTemplates != null)
        {
            try
            {
                var raw = JsonSerializer.Serialize(settings.CategoryTemplates);
                using var doc = JsonDocument.Parse(raw);
                root = doc.RootElement.Clone();
                hasJson = true;
            }
            catch { }
        }

        if (hasJson && root.ValueKind == JsonValueKind.Object)
        {
            var idKey = eventTypeId.ToString();
            var typeNameClean = (eventTypeName ?? string.Empty).Trim().ToLowerInvariant();

            JsonElement? matchedEntry = null;

            // 1. Try matching by eventTypeId (Guid string)
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name.Trim(), idKey, StringComparison.OrdinalIgnoreCase))
                {
                    matchedEntry = property.Value;
                    break;
                }
            }

            // 2. Try exact category name match
            if (matchedEntry == null && !string.IsNullOrWhiteSpace(typeNameClean))
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (string.Equals(property.Name.Trim(), typeNameClean, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedEntry = property.Value;
                        break;
                    }
                }
            }

            // 3. Fallback to "all" in CategoryTemplates
            if (matchedEntry == null)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (string.Equals(property.Name.Trim(), "all", StringComparison.OrdinalIgnoreCase))
                    {
                        matchedEntry = property.Value;
                        break;
                    }
                }
            }

            if (matchedEntry.HasValue && matchedEntry.Value.ValueKind == JsonValueKind.Object)
            {
                string? sub = null;
                string? desc = null;

                bool isReminder = string.Equals(templateType, "reminder", StringComparison.OrdinalIgnoreCase);

                if (isReminder)
                {
                    if (matchedEntry.Value.TryGetProperty("reminderSubject", out var rSub) && rSub.ValueKind == JsonValueKind.String)
                        sub = rSub.GetString();
                    if (matchedEntry.Value.TryGetProperty("reminderDescription", out var rDesc) && rDesc.ValueKind == JsonValueKind.String)
                        desc = rDesc.GetString();
                }

                if (string.IsNullOrWhiteSpace(sub))
                {
                    if (matchedEntry.Value.TryGetProperty("initialSubject", out var subProp) && subProp.ValueKind == JsonValueKind.String)
                        sub = subProp.GetString();
                    else if (matchedEntry.Value.TryGetProperty("subject", out var sProp) && sProp.ValueKind == JsonValueKind.String)
                        sub = sProp.GetString();
                }

                if (string.IsNullOrWhiteSpace(desc))
                {
                    if (matchedEntry.Value.TryGetProperty("initialDescription", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                        desc = descProp.GetString();
                    else if (matchedEntry.Value.TryGetProperty("description", out var dProp) && dProp.ValueKind == JsonValueKind.String)
                        desc = dProp.GetString();
                }

                if (!string.IsNullOrWhiteSpace(sub) || !string.IsNullOrWhiteSpace(desc))
                {
                    return (
                        !string.IsNullOrWhiteSpace(sub) ? sub : fallbackSubject,
                        !string.IsNullOrWhiteSpace(desc) ? desc : fallbackDesc
                    );
                }
            }
        }

        return (fallbackSubject, fallbackDesc);
    }

    public async Task<AvailableAmountInHandDto> GetAvailableAmountInHandAsync(Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Total Collections across all events
            var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
            var totalCollections = allContributions
                .Where(c => c.PaymentStatus == PaymentStatus.Paid ||
                            (!string.IsNullOrWhiteSpace(c.StatusName) && (
                                c.StatusName.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                                c.StatusName.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                                c.StatusName.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                                c.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                                c.StatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase))))
                .Sum(c => c.Amount);

            // 2. Total Verified/Approved Expenses across all events
            var allExpensesRaw = _expenseRepository != null
                ? await _expenseRepository.GetAllAsync(cancellationToken: cancellationToken)
                : new List<TeamContributionManagementSystem.Application.DTOs.Expenses.ExpenseDto>();

            var allExpenses = allExpensesRaw
                .Where(e => !string.IsNullOrWhiteSpace(e.Status) && (
                    e.Status.Contains("verif", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Contains("approv", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("paid", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var totalExpenses = allExpenses.Sum(e => e.Amount);

            // 3. Raw Amount In Hand (can be negative if overspent!)
            var rawAmountInHand = totalCollections - totalExpenses;
            var actualAmountInHand = Math.Max(0m, rawAmountInHand);

            // 4. Calculate unspent reservations for active/planned events using available balance
            var allEvents = await _eventRepository.GetAllAsync(null, null, cancellationToken);
            decimal totalReserved = 0m;

            foreach (var ev in allEvents.Where(e => e.UseAvailableBalance && e.AllocatedAmount > 0 && (!excludeEventId.HasValue || e.EventId != excludeEventId.Value)))
            {
                var eventExpenses = allExpenses
                    .Where(ex => string.Equals(ex.EventName?.Trim(), ev.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(ex => ex.Amount);

                // Unspent reservation is remaining allocated budget not yet consumed by actual expenses
                var unspentReservation = Math.Max(0m, ev.AllocatedAmount - eventExpenses);
                totalReserved += unspentReservation;
            }

            var availableAmountInHand = Math.Max(0m, actualAmountInHand - totalReserved);

            // 5. Calculate Total Pending Collections across active/published events
            var totalPending = allEvents.Sum(e => Math.Max(0m, e.TotalExpectedAmount - e.TotalPaidAmount));

            // 6. Find most recent previous event
            var previousEvent = allEvents
                .Where(e => (!excludeEventId.HasValue || e.EventId != excludeEventId.Value) && (e.Status == EventStatus.Completed || e.Status == EventStatus.Active || e.PublishedAt.HasValue))
                .OrderByDescending(e => e.EventDate)
                .ThenByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            // 7. Check if any planned/active event has already absorbed/scheduled deficit recovery
            decimal alreadyRecoveredDeficits = 0m;
            foreach (var ev in allEvents.Where(e => e.Status != EventStatus.Cancelled && (!excludeEventId.HasValue || e.EventId != excludeEventId.Value)))
            {
                var (_, usePrev, adjAmt, adjType, _, _) = EventMetadataHelper.UnpackDescription(ev.Description);
                if (usePrev && adjType == "DeficitRecovery" && adjAmt > 0)
                {
                    alreadyRecoveredDeficits += adjAmt;
                }
            }

            // 8. Calculate Eligible Carry-Forward / Adjustment Amount C
            decimal eligibleAdjustment = 0m;
            string adjustmentType = "None";

            if (rawAmountInHand < 0m)
            {
                // Deficit Recovery: A < 0
                // But pending collections (P) from in-progress events will cover their own
                // expenses once collected. Only the portion that exceeds pending is a true deficit.
                // Uncovered Deficit = max(0, |A| - P - alreadyRecoveredDeficits)
                var rawDeficit = Math.Abs(rawAmountInHand);
                var netUnrecoveredDeficit = Math.Max(0m, rawDeficit - totalPending - alreadyRecoveredDeficits);
                if (netUnrecoveredDeficit > 0m)
                {
                    eligibleAdjustment = netUnrecoveredDeficit;
                    adjustmentType = "DeficitRecovery";
                }
                else
                {
                    eligibleAdjustment = 0m;
                    adjustmentType = "None";
                }
            }
            else if (rawAmountInHand > 0m)
            {
                // Surplus: A >= 0 -> C = max(0, A - P)
                if (rawAmountInHand > totalPending)
                {
                    eligibleAdjustment = rawAmountInHand - totalPending;
                    adjustmentType = "SurplusCredit";
                }
                else
                {
                    eligibleAdjustment = 0m;
                    adjustmentType = "None";
                }
            }

            // Compute a display-adjusted Amount In Hand:
            // Only offset by already-recovered deficits (deficit recovery already baked into next event's budget).
            // Do NOT add totalPending here — pending collections are not yet received cash.
            // Do NOT clamp to zero — a negative value means there is still an uncovered shortfall.
            // Formula: netRawAmountInHand = rawAmountInHand + alreadyRecoveredDeficits
            // Example: rawAIH = -2600, recovered = 100  →  -2600 + 100 = -2500 (correct display)
            var netRawAmountInHand = rawAmountInHand < 0m && alreadyRecoveredDeficits > 0m
                ? rawAmountInHand + alreadyRecoveredDeficits
                : rawAmountInHand;

            return new AvailableAmountInHandDto
            {
                ActualAmountInHand = RoundingHelper.RoundToWhole(actualAmountInHand),
                TotalReservedAmount = RoundingHelper.RoundToWhole(totalReserved),
                AvailableAmountInHand = RoundingHelper.RoundToWhole(availableAmountInHand),
                RawAmountInHand = RoundingHelper.RoundToWhole(netRawAmountInHand),
                TotalPendingAmount = RoundingHelper.RoundToWhole(totalPending),
                EligibleAdjustmentAmount = RoundingHelper.RoundToWhole(eligibleAdjustment),
                TotalRecoveredDeficits = RoundingHelper.RoundToWhole(alreadyRecoveredDeficits),
                AdjustmentType = adjustmentType,
                PreviousEventName = previousEvent?.EventName,
                PreviousEventId = previousEvent?.EventId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAvailableAmountInHandAsync));
            throw;
        }
    }

    public async Task<MonthlyEventRestrictionsDto> GetMonthlyEventRestrictionsAsync(int month, int year, Guid? excludeEventId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new MonthlyEventRestrictionsDto
            {
                Month = month,
                Year = year
            };

            var activeEvents = await _eventRepository.GetActiveEventsInMonthAsync(month, year, excludeEventId, cancellationToken);
            var allMembers = await _memberRepository.GetAllAsync(cancellationToken);
            var activeMembers = allMembers.Where(m => m.IsActive && !m.IsDeleted).ToList();

            var takenTypeIds = new HashSet<Guid>();
            var takenTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var celebratingMembersList = new List<CelebratingMemberDto>();

            foreach (var ev in activeEvents)
            {
                var evTypeName = ev.EventType?.EventTypeName?.Trim() ?? string.Empty;
                var evName = ev.EventName?.Trim() ?? string.Empty;
                var isBday = evTypeName.ToLower().Contains("birthday") || evName.ToLower().Contains("birthday");

                if (isBday)
                {
                    result.IsBirthdayTaken = true;
                    if (ev.EventTypeId != Guid.Empty) takenTypeIds.Add(ev.EventTypeId);
                    if (!string.IsNullOrWhiteSpace(evTypeName)) takenTypeNames.Add(evTypeName);

                    // Every active member whose birthday falls in this month is celebrating birthday
                    foreach (var member in activeMembers)
                    {
                        if (member.DateOfBirth != default && member.DateOfBirth.Month == month)
                        {
                            celebratingMembersList.Add(new CelebratingMemberDto
                            {
                                MemberId = member.MemberId,
                                MemberName = member.Name,
                                EventTypeId = ev.EventTypeId,
                                EventTypeName = "Birthday",
                                EventId = ev.EventId,
                                EventName = ev.EventName,
                                EventDate = ev.EventDate,
                                CelebrationReason = $"Birthday Celebration ({ev.EventName})"
                            });
                        }
                    }
                }
                else
                {
                    // Non-Birthday event: extract celebrants with this event's event type
                    // Check celebrants from EventDates JSON
                    if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(ev.EventDates);
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                var typeKey = prop.Name;
                                Guid parsedTypeId = Guid.TryParse(typeKey, out var tid) ? tid : ev.EventTypeId;

                                if (prop.Value.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var subProp in prop.Value.EnumerateObject())
                                    {
                                        if (Guid.TryParse(subProp.Name, out var mId))
                                        {
                                            var m = activeMembers.FirstOrDefault(x => x.MemberId == mId);
                                            celebratingMembersList.Add(new CelebratingMemberDto
                                            {
                                                MemberId = mId,
                                                MemberName = m?.Name ?? "Member",
                                                EventTypeId = parsedTypeId,
                                                EventTypeName = evTypeName,
                                                EventId = ev.EventId,
                                                EventName = ev.EventName,
                                                EventDate = ev.EventDate,
                                                CelebrationReason = $"{evTypeName} ({ev.EventName})"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // Check celebrants from Description <!--celebrants:...-->
                    if (!string.IsNullOrWhiteSpace(ev.Description))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(ev.Description, @"<!--celebrants:(.*?)-->");
                        if (match.Success)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(match.Groups[1].Value);
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    var typeKey = prop.Name;
                                    Guid parsedTypeId = Guid.TryParse(typeKey, out var tid) ? tid : ev.EventTypeId;
                                    if (prop.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var item in prop.Value.EnumerateArray())
                                        {
                                            var mStr = item.GetString();
                                            if (Guid.TryParse(mStr, out var mId))
                                            {
                                                var m = activeMembers.FirstOrDefault(x => x.MemberId == mId);
                                                celebratingMembersList.Add(new CelebratingMemberDto
                                                {
                                                    MemberId = mId,
                                                    MemberName = m?.Name ?? "Member",
                                                    EventTypeId = parsedTypeId,
                                                    EventTypeName = !string.IsNullOrWhiteSpace(evTypeName) ? evTypeName : typeKey,
                                                    EventId = ev.EventId,
                                                    EventName = ev.EventName,
                                                    EventDate = ev.EventDate,
                                                    CelebrationReason = $"{evTypeName} ({ev.EventName})"
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }

            result.TakenEventTypeIds = takenTypeIds.ToList();
            result.TakenEventTypeNames = takenTypeNames.ToList();
            result.CelebratingMembers = celebratingMembersList;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMonthlyEventRestrictionsAsync));
            throw;
        }
    }

    private async Task ValidateMonthlyEventRulesAsync(
        CreateEventRequestDto request,
        EventType mainEventType,
        Guid? excludeEventId,
        CancellationToken cancellationToken)
    {
        var targetDate = request.EventDate.Date;
        var month = targetDate.Month;
        var year = targetDate.Year;
        var monthName = targetDate.ToString("MMMM yyyy");

        var restrictions = await GetMonthlyEventRestrictionsAsync(month, year, excludeEventId, cancellationToken);

        // 1. Detect if Birthday is requested in this event
        var isBirthdayRequested = (mainEventType?.EventTypeName?.Trim().ToLower().Contains("birthday") == true) ||
            (request.EventName?.Trim().ToLower().Contains("birthday") == true);

        if (!isBirthdayRequested && request.EventTypeIds != null && request.EventTypeIds.Count > 0)
        {
            foreach (var tId in request.EventTypeIds)
            {
                var t = tId == mainEventType?.EventTypeId ? mainEventType : await _eventTypeRepository.GetByIdAsync(tId, cancellationToken);
                if (t?.EventTypeName?.Trim().ToLower().Contains("birthday") == true)
                {
                    isBirthdayRequested = true;
                    break;
                }
            }
        }

        // Rule A: Birthday Event — Only once per calendar month
        if (isBirthdayRequested && restrictions.IsBirthdayTaken)
        {
            throw new DuplicateEventException($"A Birthday event already exists for {monthName}. Only one Birthday event is allowed per calendar month.");
        }

        // Rule B: All Other Event Types — Prevent duplicate celebrants for the same event type
        var requestedTypeCelebrants = new Dictionary<Guid, (string TypeName, HashSet<Guid> Celebrants)>();

        void AddCelebrantsForType(Guid tId, string tName, IEnumerable<Guid>? memIds)
        {
            if (tId == Guid.Empty || memIds == null) return;
            if (!requestedTypeCelebrants.TryGetValue(tId, out var entry))
            {
                entry = (tName, new HashSet<Guid>());
                requestedTypeCelebrants[tId] = entry;
            }
            foreach (var mId in memIds)
            {
                if (mId != Guid.Empty) entry.Celebrants.Add(mId);
            }
        }



        // EventTypeMembers dictionary
        if (request.EventTypeMembers != null)
        {
            foreach (var (key, list) in request.EventTypeMembers)
            {
                if (list != null && list.Count > 0)
                {
                    Guid typeId = Guid.Empty;
                    string typeName = key;
                    if (Guid.TryParse(key, out var parsedGuid))
                    {
                        typeId = parsedGuid;
                        var t = await _eventTypeRepository.GetByIdAsync(typeId, cancellationToken);
                        if (t != null) typeName = t.EventTypeName;
                    }
                    else if (mainEventType != null && mainEventType.EventTypeName.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        typeId = mainEventType.EventTypeId;
                        typeName = mainEventType.EventTypeName;
                    }
                    else
                    {
                        var allTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
                        var matched = allTypes.FirstOrDefault(t => t.EventTypeName.Equals(key, StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            typeId = matched.EventTypeId;
                            typeName = matched.EventTypeName;
                        }
                    }

                    if (typeId != Guid.Empty)
                    {
                        AddCelebrantsForType(typeId, typeName, list);
                    }
                }
            }
        }

        // Description <!--celebrants:{...}-->
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var match = System.Text.RegularExpressions.Regex.Match(request.Description, @"<!--celebrants:(.*?)-->");
            if (match.Success)
            {
                try
                {
                    using var doc = JsonDocument.Parse(match.Groups[1].Value);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var key = prop.Name;
                        var mIds = new List<Guid>();
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in prop.Value.EnumerateArray())
                            {
                                if (Guid.TryParse(item.GetString(), out var mId)) mIds.Add(mId);
                            }
                        }

                        if (mIds.Count > 0)
                        {
                            Guid typeId = Guid.Empty;
                            string typeName = key;
                            if (Guid.TryParse(key, out var parsedGuid))
                            {
                                typeId = parsedGuid;
                                var t = await _eventTypeRepository.GetByIdAsync(typeId, cancellationToken);
                                if (t != null) typeName = t.EventTypeName;
                            }
                            else if (mainEventType != null && mainEventType.EventTypeName.Equals(key, StringComparison.OrdinalIgnoreCase))
                            {
                                typeId = mainEventType.EventTypeId;
                                typeName = mainEventType.EventTypeName;
                            }

                            if (typeId != Guid.Empty)
                            {
                                AddCelebrantsForType(typeId, typeName, mIds);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // Validate: reject only when the same celebrant already has a conflicting event of THAT same event type
        foreach (var (typeId, info) in requestedTypeCelebrants)
        {
            if (info.TypeName.Trim().ToLower().Contains("birthday")) continue;

            foreach (var cId in info.Celebrants)
            {
                var conflict = restrictions.CelebratingMembers.FirstOrDefault(c =>
                    c.MemberId == cId &&
                    (c.EventTypeId == typeId ||
                     (!string.IsNullOrWhiteSpace(c.EventTypeName) && !string.IsNullOrWhiteSpace(info.TypeName) &&
                      c.EventTypeName.Trim().Equals(info.TypeName.Trim(), StringComparison.OrdinalIgnoreCase))));

                if (conflict != null)
                {
                    throw new InvalidOperationException($"Member '{conflict.MemberName}' already has a {info.TypeName} event in {monthName}. Duplicate events for the same member and event type are not allowed in the same calendar month.");
                }
            }
        }
    }

    private static string FormatEventDatesForDisplay(
        string? rawEventDates,
        DateTime fallbackDate,
        bool isBirthdayEvent = false,
        List<TeamContributionManagementSystem.Application.DTOs.Members.MemberDto>? celebrants = null)
    {
        if (!string.IsNullOrWhiteSpace(rawEventDates))
        {
            var trimmed = rawEventDates.Trim();
            if (trimmed.StartsWith("{"))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                    var formattedDates = new List<string>();

                    void ExtractDates(System.Text.Json.JsonElement element)
                    {
                        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var prop in element.EnumerateObject())
                            {
                                ExtractDates(prop.Value);
                            }
                        }
                        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in element.EnumerateArray())
                            {
                                ExtractDates(item);
                            }
                        }
                        else if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var strVal = element.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(strVal))
                            {
                                if (DateTime.TryParse(strVal, out var parsedDt))
                                {
                                    formattedDates.Add(parsedDt.ToString("MMMM dd, yyyy"));
                                }
                                else
                                {
                                    formattedDates.Add(strVal);
                                }
                            }
                        }
                    }

                    ExtractDates(doc.RootElement);

                    var distinct = formattedDates.Distinct().ToList();
                    if (distinct.Count > 0)
                    {
                        return string.Join(", ", distinct);
                    }
                }
                catch { }
            }
            else
            {
                var parts = trimmed.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => DateTime.TryParse(p, out var dt) ? dt.ToString("MMMM dd, yyyy") : p)
                    .Distinct()
                    .ToList();

                if (parts.Count > 0)
                {
                    return string.Join(", ", parts);
                }
            }
        }

        if (isBirthdayEvent && celebrants != null && celebrants.Count > 0)
        {
            var bdayDates = celebrants
                .OrderBy(c => c.DateOfBirth.Day)
                .Select(c => $"{c.DateOfBirth.Day} {c.DateOfBirth:MMM}")
                .Distinct()
                .ToList();

            if (bdayDates.Count > 0)
            {
                return string.Join(", ", bdayDates);
            }
        }

        return fallbackDate.ToString("MMMM dd, yyyy");
    }
}

