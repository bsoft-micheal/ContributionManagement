using System.Text.Json;
using AutoMapper;
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
            decimal allocatedAmount = 0m;
            if (request.UseAvailableBalance)
            {
                allocatedAmount = request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0
                    ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value)
                    : plannedBudget;

                if (allocatedAmount > plannedBudget)
                {
                    throw new InvalidOperationException($"The deducted balance amount (₹{allocatedAmount:N0}) cannot exceed the planned budget (₹{plannedBudget:N0}).");
                }

                var balanceInfo = await GetAvailableAmountInHandAsync(cancellationToken);
                if (allocatedAmount > balanceInfo.AvailableAmountInHand)
                {
                    throw new InvalidOperationException($"The selected deduction (₹{allocatedAmount:N0}) exceeds the available Amount In Hand (₹{balanceInfo.AvailableAmountInHand:N0}).");
                }
            }

            var eventItem = new Event
            {
                EventId = Guid.NewGuid(),
                EventName = (request.EventName ?? string.Empty).Trim(),
                EventTypeId = eventType.EventTypeId,
                EventDate = request.EventDate.Date,
                EventDates = !string.IsNullOrWhiteSpace(request.EventDates) ? request.EventDates.Trim() : null,
                CreatedBy = user.UserId,
                CreatedAt = DateTime.UtcNow,
                Description = (request.Description ?? string.Empty).Trim(),
                Status = request.Status != 0 ? request.Status : EventStatus.Planned,
                BaseAmount = plannedBudget,
                UseAvailableBalance = request.UseAvailableBalance,
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

            var remainingBudgetToCollect = request.UseAvailableBalance
                ? Math.Max(0m, plannedBudget - allocatedAmount)
                : plannedBudget;

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

            foreach (var c in contributions)
            {
                eventItem.Contributions.Add(c);
            }

            await _eventRepository.AddAsync(eventItem, cancellationToken);
            await _contributionRepository.AddRangeAsync(contributions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);



            // Fetch the created event to ensure it is fully committed
            var createdEvent = await GetByIdAsync(eventItem.EventId, cancellationToken);

            // Send notification emails asynchronously in the background to active contributors
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
        string eventName,
        DateTime eventDate,
        string? eventDates,
        string eventDescription,
        decimal baseAmount,
        Guid eventTypeId,
        string eventTypeName,
        HashSet<Guid> exemptCelebrantIds,
        IEnumerable<(Guid MemberId, string? Name, string? Email, decimal ContributionAmount)> participants)
    {
        // Strip internal <!--contrib:{...}--> metadata that is stored alongside the
        // user-facing description text. This must never appear in sent emails.
        eventDescription = System.Text.RegularExpressions.Regex
            .Replace(eventDescription ?? string.Empty, @"<!--contrib:[\s\S]*?-->", string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Trim();

        var particularContributors = participants
            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
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

                if (isBirthdayEvent)
                {
                    try
                    {
                        var allMonthCelebrants = await scopedMemberRepo.GetActiveBirthdaysInMonthAsync(eventDate.Month, CancellationToken.None);

                        if (exemptCelebrantIds.Count > 0)
                        {
                            targetCelebrants = allMonthCelebrants.Where(m => exemptCelebrantIds.Contains(m.MemberId)).ToList();
                            if (targetCelebrants.Count == 0)
                            {
                                var allMembers = await scopedMemberRepo.GetAllActiveAsync(CancellationToken.None);
                                targetCelebrants = allMembers.Where(m => exemptCelebrantIds.Contains(m.MemberId)).ToList();
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

                        if (targetCelebrants.Count == 0)
                        {
                            targetCelebrants = allMonthCelebrants;
                        }
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogWarning(ex, "Failed to resolve active birthday celebrants for event email dispatch");
                    }
                }

                targetCelebrants = targetCelebrants.OrderBy(c => c.DateOfBirth.Day).ToList();
                var celebrantNames = targetCelebrants.Select(m => m.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

                if (celebrantNames.Count == 0 && isBirthdayEvent)
                {
                    var nameParts = eventName.Split(new[] { '-', ':' }, 2);
                    if (nameParts.Length > 1 && !string.IsNullOrWhiteSpace(nameParts[1]))
                    {
                        var parsed = nameParts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .ToList();
                        if (parsed.Count > 0)
                        {
                            celebrantNames = parsed;
                        }
                    }
                }

                string celebrantsFormatted = celebrantNames.Count > 0 ? string.Join(", ", celebrantNames) : string.Empty;

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

                // Resolve Payment QR config for this event type
                string effectiveReceiverName = orgName;
                string effectiveUpiId = "danielrobertanto604@okicici";
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

                        // 3. Fallback to "all"
                        if (!found && root.TryGetProperty("all", out matchedElem))
                        {
                            found = true;
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

                // If uploaded image mode is configured but no data URI is in settings, try reading wwwroot gpay image
                if (string.IsNullOrWhiteSpace(uploadedQrImage) && !string.IsNullOrWhiteSpace(gpayImagePath) && File.Exists(gpayImagePath))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(gpayImagePath);
                        var ext = Path.GetExtension(gpayImagePath).TrimStart('.').ToLowerInvariant();
                        var mime = ext == "jpg" || ext == "jpeg" ? "image/jpeg" : "image/png";
                        uploadedQrImage = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                    }
                    catch { }
                }

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
                        var safeCategoryName = !string.IsNullOrWhiteSpace(eventTypeName) ? eventTypeName.Trim() : "Event";
                        var safeEventName = !string.IsNullOrWhiteSpace(eventName) ? eventName.Trim() : "Event";
                        var safeOrgName = !string.IsNullOrWhiteSpace(orgName) ? orgName.Trim() : "Unit 1A Residents Association";

                        string emailSubject = (configuredSubject ?? string.Empty)
                            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{categoryName}", safeCategoryName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventName}", safeEventName, StringComparison.OrdinalIgnoreCase)
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
                        string celebrantDatesCsv = !string.IsNullOrWhiteSpace(eventDates)
                            ? string.Join(", ", eventDates.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).Distinct())
                            : (isBirthdayEvent && targetCelebrants.Count > 0
                                ? string.Join(", ", targetCelebrants.OrderBy(c => c.DateOfBirth.Day).Select(c => $"{c.DateOfBirth.Day} {getCelebrantBirthDate(c):MMM}").Distinct())
                                : string.Empty);
                        string eventDateValueDisplay = !string.IsNullOrWhiteSpace(celebrantDatesCsv)
                            ? celebrantDatesCsv
                            : $"{eventDate:MMMM dd, yyyy}";

                        string totalAmountDisplay = isBirthdayEvent && celebrantNames.Count > 1
                            ? $"Rs.{baseAmount:F2} ({celebrantNames.Count} celebrants combined)"
                            : $"Rs.{baseAmount:F2}";

                        var frontendBaseUrl = "http://localhost:5173";
                        var portalContributionUrl = $"{frontendBaseUrl}/contributions";

                        // Generate Dynamic UPI QR Image for Contributor
                        var upiPaymentUri = $"upi://pay?pa={Uri.EscapeDataString(effectiveUpiId)}&pn={Uri.EscapeDataString(effectiveReceiverName)}&am={memberContributionAmount:F2}&cu=INR&tn={Uri.EscapeDataString($"Contribution for {safeEventName}")}";

                        var qrContentId = $"qr_{Guid.NewGuid():N}";
                        var tempDir = Path.Combine(Path.GetTempPath(), "ContributionManagement_EmailQr");
                        Directory.CreateDirectory(tempDir);
                        InlineEmailImage? inlineImage = null;

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

                        if (inlineImage == null)
                        {
                            using var qrGenerator = new QRCodeGenerator();
                            using var qrCodeData = qrGenerator.CreateQrCode(upiPaymentUri, QRCodeGenerator.ECCLevel.Q);
                            using var qrCode = new PngByteQRCode(qrCodeData);
                            var qrBytes = qrCode.GetGraphic(20);
                            var tempFilePath = Path.Combine(tempDir, $"{qrContentId}.png");
                            await File.WriteAllBytesAsync(tempFilePath, qrBytes, CancellationToken.None);
                            inlineImage = new InlineEmailImage(qrContentId, tempFilePath, "image/png");
                        }

                        var qrCodeCardHtml = $@"
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

                        bool qrInCustomDesc = (configuredDescription ?? string.Empty).Contains("{qrCode}", StringComparison.OrdinalIgnoreCase);

                        var interpolated = (configuredDescription ?? string.Empty)
                            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{categoryName}", safeCategoryName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventName}", safeEventName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
                            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
                            .Replace("{orgName}", safeOrgName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{paymentLink}", portalContributionUrl, StringComparison.OrdinalIgnoreCase)
                            .Replace("{qrCode}", qrCodeCardHtml, StringComparison.OrdinalIgnoreCase);

                        var formattedText = interpolated
                            .Replace("\r\n", "<br />")
                            .Replace("\n", "<br />");

                        string customMessageHtml = $@"<div style=""font-size: 15px; line-height: 1.7; color: #334155; margin-bottom: 24px;"">{formattedText}</div>";

                        string standaloneQrBlock = qrInCustomDesc ? string.Empty : qrCodeCardHtml;

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
                <div class=""detail-row"">
                    <span class=""detail-label"">Description:</span>
                    <span class=""detail-value"">{eventDescription}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Contribution Amount:</span>
                    <span class=""detail-value amount-highlight"">{formattedAmount}</span>
                </div>
            </div>

            {standaloneQrBlock}

            <div style=""text-align: center; margin: 30px 0 10px 0;"">
                <a href=""{portalContributionUrl}"" target=""_blank"" class=""btn-portal"">
                    Go to Contribution Page &amp; Pay
                </a>
            </div>
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
                    if (eventItem.CreatedBy != curUserGuid && !eventItem.Participants.Any(p => p.UserId == curUserGuid))
                    {
                        throw new UnauthorizedAccessException("Organizers can only edit events they created or organize.");
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

            if (request.BaseAmount <= 0)
            {
                throw new InvalidOperationException("Event cannot be saved with ₹0. Please configure budget calculation rates or enter a valid contribution amount.");
            }

            eventItem.EventName = (request.EventName ?? string.Empty).Trim();
            eventItem.EventTypeId = eventType.EventTypeId;
            eventItem.EventDate = request.EventDate.Date;
            eventItem.EventDates = !string.IsNullOrWhiteSpace(request.EventDates) ? request.EventDates.Trim() : null;
            eventItem.Description = (request.Description ?? string.Empty).Trim();
            if (request.Status != 0)
            {
                eventItem.Status = request.Status;
            }
            decimal plannedBudget = RoundingHelper.RoundToWhole(request.BaseAmount);
            eventItem.BaseAmount = plannedBudget;
            decimal allocatedAmount = 0m;
            if (request.UseAvailableBalance)
            {
                allocatedAmount = request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0
                    ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value)
                    : plannedBudget;

                if (allocatedAmount > plannedBudget)
                {
                    throw new InvalidOperationException($"The deducted balance amount (₹{allocatedAmount:N0}) cannot exceed the planned budget (₹{plannedBudget:N0}).");
                }
            }

            eventItem.BaseAmount = plannedBudget;
            eventItem.UseAvailableBalance = request.UseAvailableBalance;
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

            var remainingBudgetToCollect = request.UseAvailableBalance
                ? Math.Max(0m, plannedBudget - allocatedAmount)
                : plannedBudget;

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
                        existingList[i].IsDeleted = true;
                        existingList[i].ModifiedOn = DateTime.UtcNow;
                    }
                }
                else
                {
                    eventItem.Participants.Add(new EventParticipant
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventItem.EventId,
                        UserId = memberId,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    participantIdsToAdd.Add(memberId);
                }
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
                        existingList[i].IsDeleted = true;
                        existingList[i].ModifiedOn = DateTime.UtcNow;
                    }
                }
                else
                {
                    eventItem.Contributions.Add(new Contribution
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
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // If new participants were added, dispatch notification emails to them
            if (participantIdsToAdd.Count > 0)
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
                    if (eventItem.CreatedBy != curUserGuid && !eventItem.Participants.Any(p => p.UserId == curUserGuid))
                    {
                        throw new UnauthorizedAccessException("Organizers can only delete events they created or organize.");
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
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
            var allocatedAmount = request.UseAvailableBalance
                ? (request.AllocatedAmount.HasValue && request.AllocatedAmount.Value > 0 ? RoundingHelper.RoundToWhole(request.AllocatedAmount.Value) : plannedBudget)
                : 0m;
            var remainingBudgetToCollect = request.UseAvailableBalance
                ? Math.Max(0m, plannedBudget - allocatedAmount)
                : plannedBudget;

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
        string defaultBirthdaySubject = "Birthday Celebration Contribution - {categoryName}";
        string defaultBirthdayDesc = "Dear {memberName},\n\nWe have upcoming birthdays this month in our team! Your planned contribution for {categoryName} is {amount}, due by {dueDate}.\n\nPlease log in to the portal to view details and complete your contribution payment.\n\nWarm regards,\n{orgName}";

        string defaultGeneralSubject = "Contribution Notice - {categoryName}";
        string defaultGeneralDesc = "Dear {memberName},\n\nThis is a notification regarding your contribution for {categoryName} of {amount}, due by {dueDate}.\n\nPlease log in to the portal to view details and complete your contribution payment.\n\nThank you,\n{orgName}";

        bool isBirthday = (eventTypeName ?? string.Empty).Contains("Birthday", StringComparison.OrdinalIgnoreCase);
        string fallbackSubject = isBirthday ? defaultBirthdaySubject : defaultGeneralSubject;
        string fallbackDesc = isBirthday ? defaultBirthdayDesc : defaultGeneralDesc;

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

            // 3. Try loose category name match
            if (matchedEntry == null && !string.IsNullOrWhiteSpace(typeNameClean))
            {
                foreach (var property in root.EnumerateObject())
                {
                    var key = property.Name.Trim().ToLowerInvariant();
                    if ((typeNameClean.Contains("birthday") && key.Contains("birthday")) ||
                        (typeNameClean.Contains("dinner") && key.Contains("dinner")) ||
                        (typeNameClean.Contains("farewell") && key.Contains("farewell")) ||
                        (typeNameClean.Contains("outing") && key.Contains("outing")))
                    {
                        matchedEntry = property.Value;
                        break;
                    }
                }
            }

            // 4. Try "all" fallback
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

        // 5. Fallback to settings.EmailSubject / settings.EmailDescription
        if (!string.IsNullOrWhiteSpace(settings.EmailSubject) || !string.IsNullOrWhiteSpace(settings.EmailDescription))
        {
            return (
                !string.IsNullOrWhiteSpace(settings.EmailSubject) ? settings.EmailSubject : fallbackSubject,
                !string.IsNullOrWhiteSpace(settings.EmailDescription) ? settings.EmailDescription : fallbackDesc
            );
        }

        // 6. Default fallback to standard templates (never null!)
        return (fallbackSubject, fallbackDesc);
    }

    public async Task<AvailableAmountInHandDto> GetAvailableAmountInHandAsync(CancellationToken cancellationToken = default)
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
                                c.StatusName.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                                c.StatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase))) ||
                            (c.CashAmount.HasValue && c.CashAmount.Value > 0) ||
                            (c.UpiAmount.HasValue && c.UpiAmount.Value > 0) ||
                            c.PaymentDate != null)
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

            // 3. Actual Amount In Hand
            var actualAmountInHand = Math.Max(0m, totalCollections - totalExpenses);

            // 4. Calculate unspent reservations for active/planned events using available balance
            var allEvents = await _eventRepository.GetAllAsync(null, null, cancellationToken);
            decimal totalReserved = 0m;

            foreach (var ev in allEvents.Where(e => e.UseAvailableBalance && e.AllocatedAmount > 0))
            {
                var eventExpenses = allExpenses
                    .Where(ex => string.Equals(ex.EventName?.Trim(), ev.EventName?.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(ex => ex.Amount);

                // Unspent reservation is remaining allocated budget not yet consumed by actual expenses
                var unspentReservation = Math.Max(0m, ev.AllocatedAmount - eventExpenses);
                totalReserved += unspentReservation;
            }

            var availableAmountInHand = Math.Max(0m, actualAmountInHand - totalReserved);

            return new AvailableAmountInHandDto
            {
                ActualAmountInHand = RoundingHelper.RoundToWhole(actualAmountInHand),
                TotalReservedAmount = RoundingHelper.RoundToWhole(totalReserved),
                AvailableAmountInHand = RoundingHelper.RoundToWhole(availableAmountInHand)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAvailableAmountInHandAsync));
            throw;
        }
    }
}
