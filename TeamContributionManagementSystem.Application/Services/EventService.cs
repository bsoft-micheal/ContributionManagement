using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContributionRepository _contributionRepository;
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
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IReadOnlyCollection<EventSummaryDto>> GetAllAsync(int? month = null, int? year = null, CancellationToken cancellationToken = default)
    {
        var events = await _eventRepository.GetAllAsync(month, year, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<EventSummaryDto>>(events);
    }

    public async Task<EventDetailsDto> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventItem = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        return _mapper.Map<EventDetailsDto>(eventItem);
    }

    public async Task<EventDetailsDto> CreateAsync(Guid createdByUserId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(createdByUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Creating user not found.");

        var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Event type not found.");

        if (!eventType.IsActive)
        {
            throw new InvalidOperationException("Inactive event types cannot be used.");
        }

        var participantIds = request.ParticipantIds.Distinct().ToList();
        if (participantIds.Count == 0)
        {
            throw new InvalidOperationException("At least one participant is required.");
        }

        var members = await _memberRepository.GetByIdsAsync(participantIds, cancellationToken);
        if (members.Count != participantIds.Count)
        {
            throw new InvalidOperationException("One or more participants could not be found.");
        }

        var overrideLookup = request.ContributionOverrides
            .GroupBy(x => x.MemberId)
            .ToDictionary(x => x.Key, x => x.Last().Amount);

        var eventItem = new Event
        {
            EventId = Guid.NewGuid(),
            EventName = request.EventName.Trim(),
            EventTypeId = eventType.EventTypeId,
            EventDate = request.EventDate.Date,
            CreatedBy = user.UserId,
            Description = request.Description.Trim(),
            Status = request.Status,
            BaseAmount = request.BaseAmount
        };

        foreach (var member in members)
        {
            eventItem.Participants.Add(new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventItem.EventId,
                MemberId = member.MemberId
            });
        }

        bool isBirthdayEvent = !string.IsNullOrWhiteSpace(eventType.EventTypeName) &&
            eventType.EventTypeName.Contains("Birthday", StringComparison.OrdinalIgnoreCase);

        decimal overrideSum = request.ContributionOverrides.Sum(x => x.Amount);
        decimal splitPool = Math.Max(0m, request.BaseAmount - overrideSum);

        int fullShareCount = 0;
        int halfShareCount = 0;
        int regularParticipantsCount = 0;

        if (isBirthdayEvent)
        {
            foreach (var member in members)
            {
                if (overrideLookup.ContainsKey(member.MemberId))
                {
                    continue;
                }
                if (member.JoiningDate.AddYears(1) > eventItem.EventDate)
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

        decimal divisor = isBirthdayEvent
            ? (fullShareCount + 0.5m * halfShareCount)
            : regularParticipantsCount;

        decimal standardShare = divisor > 0 ? (splitPool / divisor) : 0m;

        var contributions = members.Select(member =>
        {
            decimal amount;
            if (overrideLookup.TryGetValue(member.MemberId, out var customAmount))
            {
                amount = customAmount;
            }
            else if (isBirthdayEvent)
            {
                bool isHalfShare = member.JoiningDate.AddYears(1) > eventItem.EventDate;
                amount = Math.Round(isHalfShare ? (standardShare * 0.5m) : standardShare, 2);
            }
            else
            {
                amount = Math.Round(standardShare, 2);
            }

            return new Contribution
            {
                ContributionId = Guid.NewGuid(),
                EventId = eventItem.EventId,
                MemberId = member.MemberId,
                Amount = amount
            };
        }).ToList();

        await _eventRepository.AddAsync(eventItem, cancellationToken);
        await _contributionRepository.AddRangeAsync(contributions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 1. First fetch the created event so event creation is guaranteed and complete
        var createdEvent = await GetByIdAsync(eventItem.EventId, cancellationToken);

        // 2. Then only the system sends the email to the particular users (contributors with positive contribution amount)
        var particularContributors = members
            .Where(m => !string.IsNullOrWhiteSpace(m.Email))
            .Select(m => new
            {
                m.MemberId,
                m.Name,
                m.Email,
                ContributionAmount = contributions.FirstOrDefault(c => c.MemberId == m.MemberId)?.Amount ?? 0m
            })
            .Where(x => x.ContributionAmount > 0)
            .ToList();

        if (particularContributors.Count > 0)
        {
            var eventId = eventItem.EventId;
            var eventName = eventItem.EventName;
            var eventDate = eventItem.EventDate;
            var eventDescription = eventItem.Description;
            var baseAmount = eventItem.BaseAmount;

            var exemptCelebrantIds = request.ContributionOverrides
                .Where(x => x.Amount == 0)
                .Select(x => x.MemberId)
                .ToHashSet();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var scopedEmailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<EventService>>();
                    var scopedMemberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
                    var scopedSettingService = scope.ServiceProvider.GetService<ISystemSettingService>();

                    string upiReceiverName = "Daniel A";
                    string upiId = "danielrobertanto604@okicici";
                    string qrImageUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=upi://pay?pa=danielrobertanto604@okicici%26pn=Daniel%20A";

                    if (scopedSettingService != null)
                    {
                        try
                        {
                            var settings = await scopedSettingService.GetSettingsAsync(CancellationToken.None);
                            if (!string.IsNullOrWhiteSpace(settings.QrReceiverName)) upiReceiverName = settings.QrReceiverName;
                            if (!string.IsNullOrWhiteSpace(settings.QrUpiId)) upiId = settings.QrUpiId;
                            if (!string.IsNullOrWhiteSpace(settings.QrImage) && settings.QrImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                qrImageUrl = settings.QrImage;
                            }
                        }
                        catch (Exception ex)
                        {
                            scopedLogger.LogWarning(ex, "Could not load payment settings for email notification; using default scanner details.");
                        }
                    }

                    var gpayImagePath = ResolveGpayImagePath();
                    if (!string.IsNullOrWhiteSpace(gpayImagePath) && File.Exists(gpayImagePath))
                    {
                        try
                        {
                            var targetDir = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                            var targetFile = Path.Combine(targetDir, "gpay.png");
                            Directory.CreateDirectory(targetDir);
                            File.Copy(gpayImagePath, targetFile, true);
                        }
                        catch { }
                    }

                    List<Member> targetCelebrants = new();

                    if (isBirthdayEvent)
                    {
                        var allMonthCelebrants = await scopedMemberRepo.GetActiveBirthdaysInMonthAsync(eventDate.Month, CancellationToken.None);

                        // 1. If exempt celebrant IDs were explicitly passed in request (e.g. from Birthday Event Dialog)
                        if (exemptCelebrantIds.Count > 0)
                        {
                            targetCelebrants = allMonthCelebrants.Where(m => exemptCelebrantIds.Contains(m.MemberId)).ToList();
                            if (targetCelebrants.Count == 0)
                            {
                                var allMembers = await scopedMemberRepo.GetAllActiveAsync(CancellationToken.None);
                                targetCelebrants = allMembers.Where(m => exemptCelebrantIds.Contains(m.MemberId)).ToList();
                            }
                        }

                        // 2. If celebrants not identified yet, check if member names appear in eventName or description
                        if (targetCelebrants.Count == 0)
                        {
                            var textToSearch = $"{eventName} {eventDescription}".ToLowerInvariant();
                            targetCelebrants = allMonthCelebrants
                                .Where(m => !string.IsNullOrWhiteSpace(m.Name) && 
                                            (textToSearch.Contains(m.Name.ToLowerInvariant()) || 
                                             textToSearch.Contains(m.MemberId.ToString().ToLowerInvariant())))
                                .ToList();
                        }

                        // 3. Fallback: all active celebrants with birthdays in this month
                        if (targetCelebrants.Count == 0)
                        {
                            targetCelebrants = allMonthCelebrants;
                        }
                    }

                    // Order celebrants by birthday day
                    targetCelebrants = targetCelebrants.OrderBy(c => c.DateOfBirth.Day).ToList();

                    var celebrantNames = targetCelebrants.Select(m => m.Name).Distinct().ToList();

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

                    // Build particular birthday event dates display
                    string birthdayDatesSummary = string.Empty;
                    string celebrantsAndDatesHtml = string.Empty;

                    if (isBirthdayEvent)
                    {
                        if (targetCelebrants.Count == 1)
                        {
                            var c = targetCelebrants[0];
                            var bdayDate = new DateTime(eventDate.Year, c.DateOfBirth.Month, Math.Min(c.DateOfBirth.Day, DateTime.DaysInMonth(eventDate.Year, c.DateOfBirth.Month)));
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
                                var bdayDate = new DateTime(eventDate.Year, c.DateOfBirth.Month, Math.Min(c.DateOfBirth.Day, DateTime.DaysInMonth(eventDate.Year, c.DateOfBirth.Month)));
                                return $"{c.Name} ({bdayDate:MMM dd})";
                            });
                            birthdayDatesSummary = string.Join(", ", summaryItems);

                            var tableRows = string.Join("", targetCelebrants.Select(c =>
                            {
                                var bdayDate = new DateTime(eventDate.Year, c.DateOfBirth.Month, Math.Min(c.DateOfBirth.Day, DateTime.DaysInMonth(eventDate.Year, c.DateOfBirth.Month)));
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

                    string singleCelebrantDateStr = string.Empty;
                    if (targetCelebrants.Count == 1)
                    {
                        var c = targetCelebrants[0];
                        var bdayDate = new DateTime(eventDate.Year, c.DateOfBirth.Month, Math.Min(c.DateOfBirth.Day, DateTime.DaysInMonth(eventDate.Year, c.DateOfBirth.Month)));
                        singleCelebrantDateStr = bdayDate.ToString("MMM dd");
                    }

                    var emailTasks = particularContributors.Select(async contributor =>
                    {
                        try
                        {
                            string emailSubject = isBirthdayEvent
                                ? (targetCelebrants.Count == 1 
                                    ? $"Birthday Celebration - {targetCelebrants[0].Name} ({singleCelebrantDateStr})"
                                    : (!string.IsNullOrWhiteSpace(celebrantsFormatted) ? $"Birthday Celebration - {celebrantsFormatted}" : $"Event Detail: {eventName}"))
                                : $"Event Detail: {eventName}";

                            string emailHeader = isBirthdayEvent ? "Birthday Celebration" : "Event Detail";

                            string introText = isBirthdayEvent && !string.IsNullOrWhiteSpace(birthdayDatesSummary)
                                ? $"We are celebrating the birthdays of our team members this month: <strong>{birthdayDatesSummary}</strong>! Here are the event details and your contribution amount:"
                                : (isBirthdayEvent && !string.IsNullOrWhiteSpace(celebrantsFormatted)
                                    ? $"We are celebrating the birthdays of our team members this month: <strong>{celebrantsFormatted}</strong>! Here are the event details and your contribution amount:"
                                    : "You have been added to a new event. Here are the event details and your contribution amount:");

                            string eventDateLabel = isBirthdayEvent ? "Celebration Date:" : "Event Date:";

                            string totalAmountDisplay = isBirthdayEvent && celebrantNames.Count > 1
                                ? $"Rs.{baseAmount:F2} ({celebrantNames.Count} celebrants combined)"
                                : $"Rs.{baseAmount:F2}";

                            var hasInlineScanner = !string.IsNullOrWhiteSpace(gpayImagePath) && File.Exists(gpayImagePath);

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
        .greeting {{
            font-size: 18px;
            font-weight: 700;
            margin-bottom: 15px;
            color: #1e1a2e;
        }}
        .intro {{
            font-size: 15px;
            line-height: 1.6;
            margin-bottom: 25px;
            color: #5b5280;
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
        .gpay-image {{
            display: block;
            width: 100%;
            max-width: 520px;
            height: auto;
            margin: 0 auto 14px;
            border-radius: 12px;
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
            <div class=""greeting"">Hello {contributor.Name},</div>
            <div class=""intro"">{introText}</div>
            
            <div class=""details-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event Name:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{eventName}</span>
                </div>
                {celebrantsAndDatesHtml}
                <div class=""detail-row"">
                    <span class=""detail-label"">{eventDateLabel}</span>
                    <span class=""detail-value"">{eventDate:MMMM dd, yyyy}</span>
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
                    <span class=""detail-value amount-highlight"">Rs.{contributor.ContributionAmount:F2}</span>
                </div>
            </div>

            <!-- Scanner & UPI Payment Card -->
            <div class=""payment-card"" style=""background: #ffffff; border: 1.5px solid #ede9fe; border-radius: 14px; padding: 18px; margin: 20px 0; text-align: center; box-shadow: 0 4px 12px rgba(124, 58, 237, 0.08);"">
                <div style=""font-family: 'Outfit', 'Inter', sans-serif; font-size: 13px; font-weight: 700; color: #4338ca; margin-bottom: 12px; letter-spacing: 0.5px; text-transform: uppercase;"">
                    Scan to Pay Contribution
                </div>
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <td align=""center"" style=""padding: 0 0 12px 0;"">
                            <img src=""{(hasInlineScanner ? "cid:gpay-banner" : qrImageUrl)}"" alt=""UPI Scanner - {upiReceiverName}"" class=""gpay-image"" style=""display:block;width:100%;max-width:300px;height:auto;margin:0 auto;border-radius:12px;border:1px solid #e0e7ff;"" />
                        </td>
                    </tr>
                    <tr>
                        <td align=""center"" style=""padding: 6px 0; font-family: 'Outfit', 'Inter', 'Segoe UI', sans-serif;"">
                            <div style=""font-size: 14px; color: #475569; margin-bottom: 6px;"">
                                Payee: <strong style=""color: #0f172a;"">{upiReceiverName}</strong>
                            </div>
                            <div style=""font-size: 14px; color: #334155; margin-bottom: 8px;"">
                                <span style=""font-weight: 600; color: #64748b; margin-right: 6px;"">UPI ID:</span>
                                <span style=""color: #312e81; background-color: #eef2ff; font-weight: 700; padding: 4px 12px; border-radius: 6px; font-family: 'Outfit', 'Courier New', monospace; letter-spacing: 0.5px; border: 1px solid #c7d2fe;"">{upiId}</span>
                            </div>
                            <div style=""font-size: 12px; color: #64748b; margin-top: 4px;"">
                                Scan with Google Pay, PhonePe, Paytm or any UPI App
                            </div>
                        </td>
                    </tr>
                </table>
            </div>
        </div>
        <div class=""footer"">
            This is an automated notification from the Team Contribution Management System.
        </div>
    </div>
</body>
</html>";

                            var inlineImages = hasInlineScanner
                                ? new[] { new InlineEmailImage("gpay-banner", gpayImagePath!, "image/png") }
                                : null;

                            await scopedEmailService.SendEmailAsync(contributor.Email, emailSubject, emailBody, inlineImages, CancellationToken.None);
                            scopedLogger.LogInformation("Successfully sent event creation email to contributor: {Email} for Event: {EventName}", contributor.Email, eventName);
                        }
                        catch (Exception ex)
                        {
                            scopedLogger.LogError(ex, "Failed to send event creation email to contributor: {Email}", contributor.Email);
                        }
                    });

                    await Task.WhenAll(emailTasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run background email tasks for new event: {EventId}", eventId);
                }
            });
        }

        return createdEvent;
    }

    public async Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var eventItem = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Event type not found.");

        eventItem.EventName = request.EventName.Trim();
        eventItem.EventTypeId = eventType.EventTypeId;
        eventItem.EventDate = request.EventDate.Date;
        eventItem.Description = request.Description.Trim();
        eventItem.Status = request.Status;
        eventItem.BaseAmount = request.BaseAmount;

        var newParticipantIds = request.ParticipantIds.Distinct().ToList();
        if (newParticipantIds.Count == 0)
        {
            throw new InvalidOperationException("At least one participant is required.");
        }

        var members = await _memberRepository.GetByIdsAsync(newParticipantIds, cancellationToken);
        if (members.Count != newParticipantIds.Count)
        {
            throw new InvalidOperationException("One or more participants could not be found.");
        }

        var overrideLookup = request.ContributionOverrides
            .GroupBy(x => x.MemberId)
            .ToDictionary(x => x.Key, x => x.Last().Amount);

        // 1. Map memberId to calculated amount
        bool isBirthdayEvent = !string.IsNullOrWhiteSpace(eventType.EventTypeName) &&
            eventType.EventTypeName.Contains("Birthday", StringComparison.OrdinalIgnoreCase);

        decimal overrideSum = request.ContributionOverrides.Sum(x => x.Amount);
        decimal splitPool = Math.Max(0m, request.BaseAmount - overrideSum);

        int fullShareCount = 0;
        int halfShareCount = 0;
        int regularParticipantsCount = 0;

        if (isBirthdayEvent)
        {
            foreach (var member in members)
            {
                if (overrideLookup.ContainsKey(member.MemberId))
                {
                    continue;
                }
                if (member.JoiningDate.AddYears(1) > eventItem.EventDate)
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

        decimal divisor = isBirthdayEvent
            ? (fullShareCount + 0.5m * halfShareCount)
            : regularParticipantsCount;

        decimal standardShare = divisor > 0 ? (splitPool / divisor) : 0m;

        var memberAmounts = members.ToDictionary(
            member => member.MemberId,
            member =>
            {
                if (overrideLookup.TryGetValue(member.MemberId, out var customAmount))
                {
                    return customAmount;
                }

                if (isBirthdayEvent)
                {
                    bool isHalfShare = member.JoiningDate.AddYears(1) > eventItem.EventDate;
                    return Math.Round(isHalfShare ? (standardShare * 0.5m) : standardShare, 2);
                }

                return Math.Round(standardShare, 2);
            }
        );

        // 2. Perform Collection Diffing for Participants
        var currentParticipantIds = eventItem.Participants.Select(p => p.MemberId).ToList();

        // Identify participants to remove
        var participantsToRemove = eventItem.Participants
            .Where(p => !newParticipantIds.Contains(p.MemberId))
            .ToList();
        if (participantsToRemove.Any())
        {
            _eventRepository.DeleteParticipants(participantsToRemove);
            foreach (var p in participantsToRemove)
            {
                eventItem.Participants.Remove(p);
            }
        }

        // Identify participants to add
        var participantIdsToAdd = newParticipantIds
            .Where(id => !currentParticipantIds.Contains(id))
            .ToList();
        foreach (var memberId in participantIdsToAdd)
        {
            eventItem.Participants.Add(new EventParticipant
            {
                Id = Guid.NewGuid(),
                EventId = eventItem.EventId,
                MemberId = memberId
            });
        }

        // 3. Perform Collection Diffing for Contributions
        var currentContributions = eventItem.Contributions.ToList();

        // Identify contributions to remove
        var contributionsToRemove = currentContributions
            .Where(c => !newParticipantIds.Contains(c.MemberId))
            .ToList();
        if (contributionsToRemove.Any())
        {
            _contributionRepository.DeleteRange(contributionsToRemove);
            foreach (var c in contributionsToRemove)
            {
                eventItem.Contributions.Remove(c);
            }
        }

        // Update amounts of existing contributions
        var existingContributions = eventItem.Contributions.ToList();
        foreach (var contribution in existingContributions)
        {
            if (memberAmounts.TryGetValue(contribution.MemberId, out var newAmount))
            {
                contribution.Amount = newAmount;
            }
        }

        // Identify new contributions to add
        var existingContributionMemberIds = existingContributions.Select(c => c.MemberId).ToList();
        var contributionMemberIdsToAdd = newParticipantIds
            .Where(id => !existingContributionMemberIds.Contains(id))
            .ToList();
        foreach (var memberId in contributionMemberIdsToAdd)
        {
            if (memberAmounts.TryGetValue(memberId, out var amount))
            {
                eventItem.Contributions.Add(new Contribution
                {
                    ContributionId = Guid.NewGuid(),
                    EventId = eventItem.EventId,
                    MemberId = memberId,
                    Amount = amount,
                    PaymentStatus = Domain.Enums.PaymentStatus.Pending
                });
            }
        }

        // Save all changes in a single transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(eventItem.EventId, cancellationToken);
    }

    public async Task DeleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventItem = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        eventItem.IsDeleted = true;

        foreach (var contribution in eventItem.Contributions)
        {
            contribution.IsDeleted = true;
        }

        _eventRepository.Update(eventItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
}
