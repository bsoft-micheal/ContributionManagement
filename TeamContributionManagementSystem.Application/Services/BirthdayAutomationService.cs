 using System.Net;
using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class BirthdayAutomationService : IBirthdayAutomationService
{
    private readonly ILogger<BirthdayAutomationService> _logger;
    private readonly IMemberRepository _memberRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventService _eventService;
    private readonly IEventRepository _eventRepository;
    private readonly IBirthdayReminderLogRepository _birthdayReminderLogRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public BirthdayAutomationService(
        ILogger<BirthdayAutomationService> logger, 
        IMemberRepository memberRepository,
        IEventTypeRepository eventTypeRepository,
        IUserRepository userRepository,
        IEventService eventService,
        IEventRepository eventRepository,
        IBirthdayReminderLogRepository birthdayReminderLogRepository,
        IEmailService emailService,
        IConfiguration configuration,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _eventTypeRepository = eventTypeRepository;
        _userRepository = userRepository;
        _eventService = eventService;
        _eventRepository = eventRepository;
        _birthdayReminderLogRepository = birthdayReminderLogRepository;
        _emailService = emailService;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateMonthlyBirthdayEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var allEventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
            var birthdayEventType = allEventTypes.FirstOrDefault(x => x.EventTypeName.Contains(CommonConstants.EventTypeNames.Birthday, StringComparison.OrdinalIgnoreCase))
                ?? allEventTypes.FirstOrDefault()
                ?? throw new KeyNotFoundException(CommonMessages.EventTypes.NoActiveConfigured);

            var adminUser = await _userRepository.GetFirstAdminAsync(cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.AdminNotFound);

            var birthdayMembers = await _memberRepository.GetActiveBirthdaysInMonthAsync(today.Month, cancellationToken);
            var activeMembers = await _memberRepository.GetAllActiveAsync(cancellationToken);
            var createdCount = 0;

            foreach (var member in birthdayMembers)
            {
                var alreadyExists = await _eventRepository.BirthdayEventExistsAsync(member.MemberId, today.Month, today.Year, cancellationToken);
                if (alreadyExists)
                {
                    continue;
                }

                var eventDate = new DateTime(today.Year, today.Month, Math.Min(member.DateOfBirth.Day, DateTime.DaysInMonth(today.Year, today.Month)), 0, 0, 0, DateTimeKind.Utc);
                var participants = activeMembers
                    .Where(x => x.MemberId != member.MemberId)
                    .Select(x => x.MemberId)
                    .ToList();

                if (participants.Count == 0)
                {
                    continue;
                }

                await _eventService.CreateAsync(adminUser.UserId, new CreateEventRequestDto
                {
                    EventName = $"{CommonConstants.EmailTemplates.BirthdaySubjectPrefix}{member.Name}",
                    EventTypeId = birthdayEventType.EventTypeId,
                    EventDate = eventDate,
                    Description = $"Auto-generated birthday contribution event for {member.Name}. {CommonConstants.Defaults.BirthdayMemberPrefix}{member.MemberId}",
                    Status = EventStatus.Planned,
                    ParticipantIds = participants,
                    BaseAmount = 0m
                }, cancellationToken);

                createdCount++;
            }

            return createdCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateMonthlyBirthdayEventsAsync));
            throw;
        }
    }

    /// <summary>
    /// Executes daily at 9:00 AM IST to send consolidated birthday reminders for tomorrow's celebrations.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessDailyBirthdayRemindersAsync(string? triggeredBy = null, CancellationToken cancellationToken = default)
    {
        var istZone = ResolveIndianTimeZone();
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        var tomorrowIst = nowIst.Date.AddDays(1);

        _logger.LogInformation("Processing daily birthday reminders for date {CelebrationDate:yyyy-MM-dd} (IST). TriggeredBy: {TriggeredBy}",
            tomorrowIst, triggeredBy ?? "ScheduledJob");

        await ProcessBirthdayRemindersForTargetDateAsync(tomorrowIst, "Scheduled", cancellationToken);
    }

    /// <summary>
    /// Incremental catch-up notification for newly joined or updated members whose celebration is tomorrow.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessCatchUpBirthdayReminderForUserAsync(Guid newUserId, CancellationToken cancellationToken = default)
    {
        var istZone = ResolveIndianTimeZone();
        var nowIst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
        var tomorrowIst = nowIst.Date.AddDays(1);

        _logger.LogInformation("Processing catch-up birthday reminder check for user {UserId} against tomorrow {Tomorrow:yyyy-MM-dd}", newUserId, tomorrowIst);

        var user = await _userRepository.GetByIdAsync(newUserId, cancellationToken);
        if (user == null || user.IsDeleted || !user.IsActive || user.IsExited || user.DateOfBirth == default)
        {
            _logger.LogInformation("User {UserId} is not eligible for birthday reminders (inactive/deleted/missing DOB).", newUserId);
            return;
        }

        // Determine the user's celebration date in tomorrow's month/year
        var celebrationDate = await ResolveMemberCelebrationDateAsync(user.UserId, user.DateOfBirth, tomorrowIst.Year, tomorrowIst.Month, cancellationToken);
        if (celebrationDate.Date != tomorrowIst.Date)
        {
            _logger.LogInformation("User {UserId}'s celebration date ({Date:yyyy-MM-dd}) does not match tomorrow ({Tomorrow:yyyy-MM-dd}). No catch-up required.",
                newUserId, celebrationDate, tomorrowIst);
            return;
        }

        await ProcessBirthdayRemindersForTargetDateAsync(tomorrowIst, "CatchUp", cancellationToken, specificCelebrantId: newUserId);
    }

    private async Task ProcessBirthdayRemindersForTargetDateAsync(
        DateTime targetDate,
        string reminderType,
        CancellationToken cancellationToken,
        Guid? specificCelebrantId = null)
    {
        var targetDateOnly = targetDate.Date;

        // 1. Retrieve all active, eligible users in the organization
        var allUsers = await _userRepository.GetAllAsync(cancellationToken);
        var activeRecipients = allUsers
            .Where(u => !u.IsDeleted && u.IsActive && !u.IsExited && !string.IsNullOrWhiteSpace(u.Email))
            .DistinctBy(u => u.Email.Trim().ToLowerInvariant())
            .ToList();

        if (activeRecipients.Count == 0)
        {
            _logger.LogInformation("No eligible recipients with valid email addresses found.");
            return;
        }

        // 2. Resolve all celebrants whose celebration date is targetDateOnly
        var candidateUsers = allUsers
            .Where(u => !u.IsDeleted && u.IsActive && !u.IsExited && u.DateOfBirth != default)
            .ToList();

        if (specificCelebrantId.HasValue)
        {
            candidateUsers = candidateUsers.Where(u => u.UserId == specificCelebrantId.Value).ToList();
        }

        var matchingCelebrants = new List<CelebrantInfo>();
        foreach (var candidate in candidateUsers)
        {
            var celDate = await ResolveMemberCelebrationDateAsync(candidate.UserId, candidate.DateOfBirth, targetDateOnly.Year, targetDateOnly.Month, cancellationToken);
            if (celDate.Date == targetDateOnly)
            {
                var displayName = !string.IsNullOrWhiteSpace(candidate.FullName) ? candidate.FullName.Trim() : candidate.Username?.Trim() ?? "Team Member";
                matchingCelebrants.Add(new CelebrantInfo(candidate.UserId, displayName, candidate.Email));
            }
        }

        if (matchingCelebrants.Count == 0)
        {
            _logger.LogInformation("No birthday celebrants found for date {Date:yyyy-MM-dd}.", targetDateOnly);
            return;
        }

        // 3. For each recipient, determine which celebrants they have NOT yet been notified about
        var formattedCelebrationDate = targetDateOnly.ToString("dd MMMM yyyy");
        var orgName = _configuration["SystemSettings:OrganizationName"] ?? "Contribution Management Team";

        foreach (var recipient in activeRecipients)
        {
            var pendingCelebrantsForRecipient = new List<CelebrantInfo>();

            foreach (var celebrant in matchingCelebrants)
            {
                var alreadyNotified = await _birthdayReminderLogRepository.HasBeenNotifiedAsync(
                    targetDateOnly,
                    celebrant.UserId,
                    recipient.UserId,
                    cancellationToken);

                if (!alreadyNotified)
                {
                    pendingCelebrantsForRecipient.Add(celebrant);
                }
            }

            if (pendingCelebrantsForRecipient.Count == 0)
            {
                continue; // All celebrants have already been notified to this recipient
            }

            // Consolidate celebrant names for this recipient
            var celebrantNamesList = string.Join(", ", pendingCelebrantsForRecipient.Select(c => c.DisplayName));
            var celebrantNamesHtml = string.Join("<br/>", pendingCelebrantsForRecipient.Select(c => $"&bull; <strong>{WebUtility.HtmlEncode(c.DisplayName)}</strong>"));

            var subject = "Birthday Celebration Reminder – Tomorrow";
            var body = BuildBirthdayReminderEmailBody(formattedCelebrationDate, celebrantNamesList, celebrantNamesHtml, orgName);

            try
            {
                await _emailService.SendEmailAsync(
                    recipient.Email,
                    subject,
                    body,
                    cancellationToken: cancellationToken);

                // Record deduplication log entries for each celebrant for this recipient
                foreach (var celebrant in pendingCelebrantsForRecipient)
                {
                    await _birthdayReminderLogRepository.AddAsync(new BirthdayReminderLog
                    {
                        ReminderLogId = Guid.NewGuid(),
                        ReminderType = reminderType,
                        CelebrationDate = DateTime.SpecifyKind(targetDateOnly, DateTimeKind.Utc),
                        CelebrantUserId = celebrant.UserId,
                        RecipientUserId = recipient.UserId,
                        RecipientEmail = recipient.Email.Trim().ToLowerInvariant(),
                        Status = "Sent",
                        SentAt = DateTime.UtcNow
                    }, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Sent birthday reminder to {RecipientEmail} for celebrant(s): {Celebrants}", recipient.Email, celebrantNamesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send birthday reminder email to {RecipientEmail}", recipient.Email);

                // Record failed log entries so failures are visible and can be safely retried
                foreach (var celebrant in pendingCelebrantsForRecipient)
                {
                    await _birthdayReminderLogRepository.AddAsync(new BirthdayReminderLog
                    {
                        ReminderLogId = Guid.NewGuid(),
                        ReminderType = reminderType,
                        CelebrationDate = DateTime.SpecifyKind(targetDateOnly, DateTimeKind.Utc),
                        CelebrantUserId = celebrant.UserId,
                        RecipientUserId = recipient.UserId,
                        RecipientEmail = recipient.Email.Trim().ToLowerInvariant(),
                        Status = "Failed",
                        SentAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message
                    }, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Resolves actual configured celebration date for a member in a specific year and month.
    /// First inspects existing published/planned Birthday event's EventDates mapping.
    /// If not explicitly configured, falls back to the member's DateOfBirth calendar day.
    /// </summary>
    private async Task<DateTime> ResolveMemberCelebrationDateAsync(
        Guid userId,
        DateTime? dateOfBirth,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check published and non-deleted monthly Birthday events for the target month
            var monthlyEvents = await _eventRepository.GetAllAsync(month, year, cancellationToken);
            var birthdayEvents = monthlyEvents
                .Where(e => e.EventTypeName.Contains(CommonConstants.EventTypeNames.Birthday, StringComparison.OrdinalIgnoreCase) ||
                            (e.EventTypeIds != null && e.EventTypeIds.Any()))
                .ToList();

            foreach (var ev in birthdayEvents)
            {
                if (!string.IsNullOrWhiteSpace(ev.EventDates) && ev.EventDates.Trim().StartsWith("{"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(ev.EventDates);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var mProp in prop.Value.EnumerateObject())
                                {
                                    if (Guid.TryParse(mProp.Name, out var mUserId) && mUserId == userId)
                                    {
                                        var dateVal = mProp.Value.GetString();
                                        if (DateTime.TryParse(dateVal, out var customDate))
                                        {
                                            return customDate.Date;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Fallback to DOB */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve custom celebration date from events for user {UserId}", userId);
        }

        // Standard DOB rule: same day in target month/year
        var maxDaysInMonth = DateTime.DaysInMonth(year, month);
        var day = Math.Min(dateOfBirth?.Day ?? 1, maxDaysInMonth);
        return new DateTime(year, month, day).Date;
    }

    private static string BuildBirthdayReminderEmailBody(
        string formattedCelebrationDate,
        string celebrantNamesText,
        string celebrantNamesHtml,
        string orgName)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Birthday Celebration Reminder – Tomorrow</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f1f5f9;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            color: #1e293b;
        }}
        .email-container {{
            max-width: 600px;
            margin: 30px auto;
            background: #ffffff;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.08);
            border: 1px solid #e2e8f0;
        }}
        .email-header {{
            background: linear-gradient(135deg, #ec4899 0%, #8b5cf6 100%);
            padding: 36px 30px;
            text-align: center;
            color: #ffffff;
        }}
        .email-header .badge {{
            display: inline-block;
            background: rgba(255, 255, 255, 0.2);
            color: #ffffff;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 1.5px;
            padding: 6px 14px;
            border-radius: 20px;
            text-transform: uppercase;
            margin-bottom: 12px;
            border: 1px solid rgba(255, 255, 255, 0.3);
        }}
        .email-header h1 {{
            margin: 0 0 6px 0;
            font-size: 24px;
            font-weight: 800;
            letter-spacing: -0.5px;
        }}
        .email-body {{
            padding: 32px 30px;
        }}
        .greeting {{
            font-size: 16px;
            font-weight: 600;
            color: #0f172a;
            margin-bottom: 16px;
        }}
        .message-intro {{
            font-size: 14px;
            line-height: 1.6;
            color: #475569;
            margin-bottom: 24px;
        }}
        .details-card {{
            background-color: #faf9fd;
            border: 1px solid rgba(139, 92, 246, 0.15);
            border-radius: 10px;
            padding: 24px;
            margin-bottom: 25px;
        }}
        .detail-row {{
            margin-bottom: 16px;
            border-bottom: 1px dashed rgba(139, 92, 246, 0.15);
            padding-bottom: 16px;
        }}
        .detail-row:last-child {{
            margin-bottom: 0;
            border-bottom: none;
            padding-bottom: 0;
        }}
        .detail-label {{
            font-weight: 700;
            color: #6b21a8;
            display: inline-block;
            width: 160px;
            vertical-align: top;
        }}
        .detail-value {{
            color: #1e1a2e;
            display: inline-block;
            font-size: 15px;
        }}
        .closing-text {{
            font-size: 14px;
            color: #334155;
            line-height: 1.6;
            margin-bottom: 24px;
        }}
        .email-footer {{
            background: #f8fafc;
            padding: 20px 30px;
            text-align: center;
            font-size: 12px;
            color: #64748b;
            border-top: 1px solid #e2e8f0;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""email-header"">
            <span class=""badge"">&#127874; Birthday Celebration</span>
            <h1>Birthday Celebration Reminder</h1>
        </div>
        <div class=""email-body"">
            <div class=""greeting"">Hello Team,</div>
            <div class=""message-intro"">
                This is a friendly reminder that we will be celebrating the following birthday(s) tomorrow.
            </div>

            <div class=""details-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Celebration Date:</span>
                    <span class=""detail-value""><strong>{formattedCelebrationDate}</strong></span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Celebrant(s):</span>
                    <span class=""detail-value"">{celebrantNamesHtml}</span>
                </div>
            </div>

            <div class=""closing-text"">
                Let's join together to wish them a wonderful birthday and make their day special!
            </div>

            <p style=""font-size: 14px; color: #475569; margin: 0;"">
                Regards,<br />
                <strong>Contribution Management Team</strong>
            </p>
        </div>
        <div class=""email-footer"">
            This is an automated notification from {orgName}.
        </div>
    </div>
</body>
</html>";
    }

    private static TimeZoneInfo ResolveIndianTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    private sealed record CelebrantInfo(Guid UserId, string DisplayName, string Email);
}
