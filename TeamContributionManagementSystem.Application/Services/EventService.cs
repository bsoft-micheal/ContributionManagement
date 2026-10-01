using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Settings;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EventService(
        IEventRepository eventRepository,
        IEventTypeRepository eventTypeRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository,
        IContributionRepository contributionRepository,
        IBudgetCalculationRepository budgetCalculationRepository,
        IExpenseRepository expenseRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        ILogger<EventService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _eventRepository = eventRepository;
        _eventTypeRepository = eventTypeRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _contributionRepository = contributionRepository;
        _budgetCalculationRepository = budgetCalculationRepository;
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventRepository.GetAllAsync(month, year, cancellationToken);
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

            if (members.Count == 0)
            {
                throw new InvalidOperationException(CommonMessages.Events.ParticipantsNotFound);
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
                BaseAmount = request.BaseAmount
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

            var memberAmounts = CalculateMemberContributionAmounts(
                eventType,
                eventItem.EventDate,
                request.BaseAmount,
                members,
                request.ContributionOverrides ?? new List<ContributionOverrideDto>());

            var creatorDisplayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

            var contributions = members.Select(member => new Contribution
            {
                ContributionId = Guid.NewGuid(),
                EventId = eventItem.EventId,
                UserId = member.MemberId,
                MemberId = member.MemberId,
                Amount = memberAmounts.TryGetValue(member.MemberId, out var amount) ? amount : 0m,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMode = PaymentMode.None,
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

            // Auto-create expenses from active BudgetCalculation items for the event types
            try
            {
                var targetTypeIds = (request.EventTypeIds != null && request.EventTypeIds.Count > 0)
                    ? request.EventTypeIds.Distinct().ToList()
                    : new List<Guid> { eventType.EventTypeId };

                var allBudgetItems = new List<(BudgetCalculation Item, string CategoryName)>();
                foreach (var tid in targetTypeIds)
                {
                    var items = await _budgetCalculationRepository.GetByEventTypeIdAsync(tid, cancellationToken);
                    var typeObj = tid == eventType.EventTypeId ? eventType : await _eventTypeRepository.GetByIdAsync(tid, cancellationToken);
                    var catName = typeObj?.EventTypeName ?? eventType.EventTypeName;
                    foreach (var item in items)
                    {
                        allBudgetItems.Add((item, catName));
                    }
                }

                if (allBudgetItems.Count > 0)
                {
                    var creatorName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

                    var autoExpenses = allBudgetItems.Select(b => new Expense
                    {
                        ExpenseId = Guid.NewGuid(),
                        EventName = eventItem.EventName,
                        Category = b.CategoryName,
                        Amount = b.Item.Rate,
                        ExpenseDate = eventItem.EventDate,
                        Status = "Pending",
                        SubmittedBy = creatorName,
                        Description = b.Item.ExpenseItem,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedBy = user.UserId,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    foreach (var expense in autoExpenses)
                        await _expenseRepository.AddAsync(expense, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Auto-created {Count} expense(s) for event '{EventName}'",
                        autoExpenses.Count, eventItem.EventName);
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: log and continue — event is already saved
                _logger.LogWarning(ex,
                    "Failed to auto-create expenses for event '{EventName}'", eventItem.EventName);
            }

            // Fetch the created event to ensure it is fully committed
            var createdEvent = await GetByIdAsync(eventItem.EventId, cancellationToken);

            // Send notification emails asynchronously in the background to active contributors
            var participantData = members.Select(m => (
                MemberId: m.MemberId,
                Name: m.Name,
                Email: m.Email,
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

                string upiReceiverName = CommonConstants.Defaults.DefaultPayeeName;
                string upiId = CommonConstants.Defaults.DefaultUpiId;
                string qrImageUrl = string.Empty;
                SystemSettingsDto? systemSettings = null;

                if (scopedSettingService != null)
                {
                    try
                    {
                        systemSettings = await scopedSettingService.GetSettingsAsync(CancellationToken.None);
                        if (!string.IsNullOrWhiteSpace(systemSettings.QrReceiverName)) upiReceiverName = systemSettings.QrReceiverName;
                        if (!string.IsNullOrWhiteSpace(systemSettings.QrUpiId)) upiId = systemSettings.QrUpiId;
                        if (!string.IsNullOrWhiteSpace(systemSettings.QrImage) && systemSettings.QrImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            qrImageUrl = systemSettings.QrImage;
                        }
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
                            ? eventDates
                            : (isBirthdayEvent && targetCelebrants.Count > 0
                                ? string.Join(", ", targetCelebrants.OrderBy(c => c.DateOfBirth.Day).Select(c => $"{c.DateOfBirth.Day} {getCelebrantBirthDate(c):MMM}"))
                                : string.Empty);
                        string eventDateValueDisplay = !string.IsNullOrWhiteSpace(celebrantDatesCsv)
                            ? celebrantDatesCsv
                            : $"{eventDate:MMMM dd, yyyy}";

                        string totalAmountDisplay = isBirthdayEvent && celebrantNames.Count > 1
                            ? $"Rs.{baseAmount:F2} ({celebrantNames.Count} celebrants combined)"
                            : $"Rs.{baseAmount:F2}";

                        var upiPaymentUri = $"upi://pay?pa={upiId}&pn={Uri.EscapeDataString(upiReceiverName)}&am={memberContributionAmount:F2}&cu=INR&tn={Uri.EscapeDataString("Contribution for " + safeEventName)}";
                        var memberQrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=260x260&margin=8&data={Uri.EscapeDataString(upiPaymentUri)}";
                        var frontendBaseUrl = "http://localhost:5173";
                        var confirmPaymentUrl = $"{frontendBaseUrl}/confirm-payment?eventId={eventId}&memberId={contributor.MemberId}&amount={memberContributionAmount:F2}";

                        var paymentLinkInline = upiPaymentUri;
                        var qrImageInlineTag = $@"
                            <div style=""text-align: center; margin: 18px 0;"">
                                <div style=""display: inline-block; padding: 12px; background: #ffffff; border: 2px solid #7c3aed; border-radius: 12px; box-shadow: 0 4px 12px rgba(124, 58, 237, 0.12);"">
                                    <a href=""{upiPaymentUri}"" style=""text-decoration: none; display: block;"">
                                        <img src=""{memberQrCodeUrl}"" alt=""UPI Payment QR Code - {upiReceiverName}"" width=""220"" height=""220"" style=""display: block; margin: 0 auto; border-radius: 6px;"" />
                                    </a>
                                </div>
                                <div style=""font-size: 12px; color: #64748b; margin-top: 6px;"">UPI ID: <strong style=""color: #312e81;"">{upiId}</strong> ({upiReceiverName})</div>
                            </div>";

                        bool hasCustomQrCode = (configuredDescription ?? string.Empty).Contains("{qrCode}", StringComparison.OrdinalIgnoreCase);

                        var interpolated = (configuredDescription ?? string.Empty)
                            .Replace("{memberName}", memberName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{categoryName}", safeCategoryName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{eventName}", safeEventName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{amount}", formattedAmount, StringComparison.OrdinalIgnoreCase)
                            .Replace("{dueDate}", formattedDueDate, StringComparison.OrdinalIgnoreCase)
                            .Replace("{orgName}", safeOrgName, StringComparison.OrdinalIgnoreCase)
                            .Replace("{paymentLink}", paymentLinkInline, StringComparison.OrdinalIgnoreCase)
                            .Replace("{qrCode}", qrImageInlineTag, StringComparison.OrdinalIgnoreCase);

                        var formattedText = interpolated
                            .Replace("\r\n", "<br />")
                            .Replace("\n", "<br />");

                        string customMessageHtml = $@"<div style=""font-size: 15px; line-height: 1.7; color: #334155; margin-bottom: 24px;"">{formattedText}</div>";

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
        .payment-card {{
            background: linear-gradient(to right, #ffffff, #faf9fd);
            border: 1.5px solid #e9e6f5;
            border-radius: 10px;
            padding: 18px 24px;
            margin-bottom: 25px;
            box-shadow: 0 4px 10px rgba(74, 63, 107, 0.04);
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

            {(hasCustomQrCode ? $@"
            <div style=""margin: 20px 0; padding: 18px; background: #ffffff; border: 1.5px solid #ede9fe; border-radius: 12px; text-align: center;"">
                <a href=""{upiPaymentUri}"" style=""display: inline-block; background: #7c3aed; color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 700; padding: 9px 24px; border-radius: 8px;"">Pay {formattedAmount} via UPI App</a>
                <div style=""margin-top: 16px; padding-top: 14px; border-top: 1.5px dashed #e2e8f0;"">
                    <div style=""font-size: 13px; font-weight: 700; color: #0f172a; margin-bottom: 4px;"">Already Paid? Submit Payment Proof</div>
                    <div style=""font-size: 12px; color: #64748b; margin-bottom: 10px;"">Submit your 12-digit UPI Reference / UTR Number to automatically update your payment status.</div>
                    <a href=""{confirmPaymentUrl}"" target=""_blank"" style=""display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 700; padding: 10px 22px; border-radius: 8px; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.25);"">&#x2705; I Have Paid — Submit UTR / Ref No.</a>
                </div>
            </div>" : $@"
            <!-- Dynamic UPI QR Scanner Card -->
            <div class=""payment-card"" style=""background: #ffffff; border: 1.5px solid #ede9fe; border-radius: 14px; padding: 20px; margin: 22px 0; text-align: center; box-shadow: 0 4px 14px rgba(124, 58, 237, 0.08);"">
                <div style=""font-family: 'Outfit', 'Inter', sans-serif; font-size: 13px; font-weight: 700; color: #4338ca; margin-bottom: 12px; letter-spacing: 0.5px; text-transform: uppercase;"">
                    Scan QR Code to Pay via UPI
                </div>
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <td align=""center"" style=""padding: 0 0 14px 0;"">
                            <div style=""display: inline-block; padding: 12px; background: #ffffff; border: 2px solid #7c3aed; border-radius: 12px; box-shadow: 0 2px 8px rgba(124, 58, 237, 0.12);"">
                                <a href=""{upiPaymentUri}"" style=""text-decoration: none; display: block;"">
                                    <img src=""{memberQrCodeUrl}"" alt=""UPI Payment QR Code - {upiReceiverName}"" width=""220"" height=""220"" style=""display: block; margin: 0 auto; border-radius: 6px;"" />
                                </a>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td align=""center"" style=""padding: 4px 0; font-family: 'Outfit', 'Inter', 'Segoe UI', sans-serif;"">
                            <div style=""font-size: 14px; color: #475569; margin-bottom: 6px;"">
                                Payee: <strong style=""color: #0f172a;"">{upiReceiverName}</strong>
                            </div>
                            <div style=""font-size: 14px; color: #334155; margin-bottom: 8px;"">
                                <span style=""font-weight: 600; color: #64748b; margin-right: 6px;"">UPI ID:</span>
                                <span style=""color: #312e81; background-color: #eef2ff; font-weight: 700; padding: 4px 12px; border-radius: 6px; font-family: 'Outfit', 'Courier New', monospace; letter-spacing: 0.5px; border: 1px solid #c7d2fe;"">{upiId}</span>
                            </div>
                            <div style=""font-size: 12px; color: #64748b; margin-top: 4px;"">
                                Scan with Google Pay, PhonePe, Paytm, or Camera to pay automatically.
                            </div>
                            <div style=""margin-top: 10px;"">
                                <a href=""{upiPaymentUri}"" style=""display: inline-block; background: #7c3aed; color: #ffffff; text-decoration: none; font-size: 12.5px; font-weight: 700; padding: 7px 18px; border-radius: 6px;"">Open UPI App ({formattedAmount})</a>
                            </div>

                            <!-- One-Click Confirmation Section -->
                            <div style=""margin-top: 20px; padding-top: 16px; border-top: 1.5px dashed #e2e8f0; text-align: center;"">
                                <div style=""font-size: 13.5px; font-weight: 700; color: #0f172a; margin-bottom: 4px;"">
                                    Already Paid? Submit Payment Proof
                                </div>
                                <div style=""font-size: 12px; color: #64748b; margin-bottom: 12px;"">
                                    Click below to submit your 12-digit UPI Reference / UTR Number to automatically update your payment status.
                                </div>
                                <a href=""{confirmPaymentUrl}"" target=""_blank"" style=""display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #ffffff; text-decoration: none; font-size: 13px; font-weight: 700; padding: 10px 22px; border-radius: 8px; box-shadow: 0 4px 12px rgba(16, 185, 129, 0.25);"">
                                    &#x2705; I Have Paid — Submit UTR / Ref No.
                                </a>
                            </div>
                        </td>
                    </tr>
                </table>
            </div>")}
        </div>
        <div class=""footer"">
            This is an automated notification from {safeOrgName}.
        </div>
    </div>
</body>
</html>";

                        await scopedEmailService.SendEmailAsync(contributor.Email, emailSubject, emailBody, null, CancellationToken.None);
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

            var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NotFound);

            eventItem.EventName = (request.EventName ?? string.Empty).Trim();
            eventItem.EventTypeId = eventType.EventTypeId;
            eventItem.EventDate = request.EventDate.Date;
            eventItem.EventDates = !string.IsNullOrWhiteSpace(request.EventDates) ? request.EventDates.Trim() : null;
            eventItem.Description = (request.Description ?? string.Empty).Trim();
            if (request.Status != 0)
            {
                eventItem.Status = request.Status;
            }
            eventItem.BaseAmount = request.BaseAmount;
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

            var memberAmounts = CalculateMemberContributionAmounts(
                eventType,
                eventItem.EventDate,
                request.BaseAmount,
                members,
                request.ContributionOverrides ?? new List<ContributionOverrideDto>());

            // 1. Synchronize Event Participants
            var currentParticipants = eventItem.Participants.Where(p => !p.IsDeleted).ToList();
            var currentParticipantIds = currentParticipants.Select(p => p.MemberId).ToHashSet();

            // Remove participants no longer in the request
            var participantsToRemove = currentParticipants
                .Where(p => !newParticipantIds.Contains(p.MemberId))
                .ToList();
            if (participantsToRemove.Count > 0)
            {
                _eventRepository.DeleteParticipants(participantsToRemove);
                foreach (var p in participantsToRemove)
                {
                    eventItem.Participants.Remove(p);
                }
            }

            // Add newly selected participants
            var participantIdsToAdd = newParticipantIds
                .Where(id => !currentParticipantIds.Contains(id))
                .ToList();
            foreach (var memberId in participantIdsToAdd)
            {
                eventItem.Participants.Add(new EventParticipant
                {
                    Id = Guid.NewGuid(),
                    EventId = eventItem.EventId,
                    MemberId = memberId,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. Synchronize Contributions
            var activeContributions = eventItem.Contributions.Where(c => !c.IsDeleted).ToList();
            var memberContribLookup = activeContributions
                .GroupBy(c => c.MemberId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Remove contributions for removed participants
            var contributionsToRemove = activeContributions
                .Where(c => !newParticipantIds.Contains(c.MemberId))
                .ToList();
            if (contributionsToRemove.Count > 0)
            {
                _contributionRepository.DeleteRange(contributionsToRemove);
                foreach (var c in contributionsToRemove)
                {
                    c.IsDeleted = true;
                    c.ModifiedOn = DateTime.UtcNow;
                    eventItem.Contributions.Remove(c);
                }
            }

            // Update existing contributions or add new ones
            foreach (var memberId in newParticipantIds)
            {
                if (!memberAmounts.TryGetValue(memberId, out var amount))
                {
                    continue;
                }

                if (memberContribLookup.TryGetValue(memberId, out var existingList) && existingList.Count > 0)
                {
                    var primary = existingList.First();
                    if (!contributionsToRemove.Contains(primary))
                    {
                        primary.Amount = amount;
                        primary.ModifiedOn = DateTime.UtcNow;
                    }

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
                        MemberId = memberId,
                        Amount = amount,
                        PaymentStatus = PaymentStatus.Pending,
                        PaymentMode = PaymentMode.None,
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

            eventItem.IsDeleted = true;
            eventItem.ModifiedOn = DateTime.UtcNow;

            if (eventItem.Contributions != null)
            {
                foreach (var contribution in eventItem.Contributions)
                {
                    contribution.IsDeleted = true;
                    contribution.ModifiedOn = DateTime.UtcNow;
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
        var overrideLookup = (overrides ?? Array.Empty<ContributionOverrideDto>())
            .GroupBy(x => x.MemberId)
            .ToDictionary(x => x.Key, x => x.Last().Amount);

        bool hasTenureRule = eventType.HasTenureRule;
        decimal thresholdYears = eventType.TenureThresholdYears > 0 ? eventType.TenureThresholdYears : 1.0m;
        decimal newEntrantRatio = (eventType.NewEntrantSharePercentage > 0 ? eventType.NewEntrantSharePercentage : 50.0m) / 100.0m;
        decimal standardRatio = (eventType.StandardSharePercentage > 0 ? eventType.StandardSharePercentage : 100.0m) / 100.0m;

        decimal overrideSum = overrides?.Sum(x => x.Amount) ?? 0m;
        decimal splitPool = Math.Max(0m, baseAmount - overrideSum);

        int fullShareCount = 0;
        int halfShareCount = 0;
        int regularParticipantsCount = 0;

        if (hasTenureRule)
        {
            foreach (var member in members)
            {
                if (overrideLookup.ContainsKey(member.MemberId))
                {
                    continue;
                }

                double tenureDays = (eventDate.Date - member.JoiningDate.Date).TotalDays;
                double tenureYears = tenureDays / 365.25;
                if (tenureYears < (double)thresholdYears)
                {
                    halfShareCount++;
                }
                else
                {
                    fullShareCount++;
                }
            }
        }
        else
        {
            regularParticipantsCount = members.Count(m => !overrideLookup.ContainsKey(m.MemberId));
        }

        decimal divisor = hasTenureRule
            ? (fullShareCount * standardRatio + halfShareCount * newEntrantRatio)
            : regularParticipantsCount;

        decimal standardShare = divisor > 0 ? (splitPool / divisor) : 0m;

        return members.ToDictionary(
            member => member.MemberId,
            member =>
            {
                if (overrideLookup.TryGetValue(member.MemberId, out var customAmount))
                {
                    return customAmount;
                }

                if (hasTenureRule)
                {
                    double tenureDays = (eventDate.Date - member.JoiningDate.Date).TotalDays;
                    double tenureYears = tenureDays / 365.25;
                    bool isNewEntrant = tenureYears < (double)thresholdYears;
                    decimal shareRatio = isNewEntrant ? newEntrantRatio : standardRatio;
                    return Math.Round(standardShare * shareRatio, 2);
                }

                return Math.Round(standardShare, 2);
            });
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
        string defaultBirthdayDesc = "Dear {memberName},\n\nWe have upcoming birthdays this month in our team! Your planned contribution for {categoryName} is {amount}, due by {dueDate}.\n\nPlease scan the dynamic UPI QR code below or tap the payment link to contribute:\n{paymentLink}\n\n{qrCode}\n\nWarm regards,\n{orgName}";

        string defaultGeneralSubject = "Contribution Notice - {categoryName}";
        string defaultGeneralDesc = "Dear {memberName},\n\nThis is a notification regarding your contribution for {categoryName} of {amount}, due by {dueDate}.\n\nPlease scan the attached dynamic UPI QR code or click the payment link to pay:\n{paymentLink}\n\n{qrCode}\n\nThank you,\n{orgName}";

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
}
