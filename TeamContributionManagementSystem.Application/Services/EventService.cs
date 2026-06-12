using AutoMapper;
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

    public EventService(
        IEventRepository eventRepository,
        IEventTypeRepository eventTypeRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository,
        IContributionRepository contributionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IEmailService emailService,
        ILogger<EventService> logger)
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

        var contributions = members.Select(member => 
        {
            var baseAmount = overrideLookup.TryGetValue(member.MemberId, out var amount)
                ? amount
                : request.BaseAmount;

            // Apply 50% reduction for members with less than 1 year of tenure
            if (member.JoiningDate.AddYears(1) > eventItem.EventDate)
            {
                baseAmount *= 0.5m;
            }

            return new Contribution
            {
                ContributionId = Guid.NewGuid(),
                EventId = eventItem.EventId,
                MemberId = member.MemberId,
                Amount = baseAmount
            };
        }).ToList();

        await _eventRepository.AddAsync(eventItem, cancellationToken);
        await _contributionRepository.AddRangeAsync(contributions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var emailTasks = members.Select(async member =>
            {
                var contributionAmount = contributions.FirstOrDefault(c => c.MemberId == member.MemberId)?.Amount ?? 0m;
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
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(74, 63, 107, 0.08);
            border: 1px solid rgba(74, 63, 107, 0.08);
        }}
        .header {{
            background: linear-gradient(135deg, #4a3f6b 0%, #2d2550 100%);
            padding: 30px 20px;
            text-align: center;
            color: #ffffff;
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
            background-color: #ffffff;
            border: 1px solid rgba(74, 63, 107, 0.08);
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 25px;
        }}
        .detail-row {{
            margin-bottom: 12px;
            border-bottom: 1px solid rgba(74, 63, 107, 0.05);
            padding-bottom: 12px;
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
            width: 150px;
        }}
        .detail-value {{
            color: #1e1a2e;
            display: inline-block;
        }}
        .amount-highlight {{
            font-size: 18px;
            color: #4a3f6b;
            font-weight: 800;
        }}
        .payment-card {{
            background-color: #ffffff;
            border: 1px solid rgba(74, 63, 107, 0.08);
            border-radius: 8px;
            padding: 16px 20px;
            margin-bottom: 25px;
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
            <h1>New Event Notification</h1>
        </div>
        <div class=""content"">
            <div class=""greeting"">Hello {member.Name},</div>
            <div class=""intro"">You have been added to a new event. Here are the event details and your contribution amount:</div>
            
            <div class=""details-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event Name:</span>
                    <span class=""detail-value"" style=""font-weight: 700;"">{eventItem.EventName}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Date:</span>
                    <span class=""detail-value"">{eventItem.EventDate:MMMM dd, yyyy}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Description:</span>
                    <span class=""detail-value"">{eventItem.Description}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Contribution Amount:</span>
                    <span class=""detail-value amount-highlight"">Rs.{contributionAmount:F2}</span>
                </div>
            </div>

            <!-- GPay Payment Card -->
            <div class=""payment-card"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <td align=""left"" style=""vertical-align: middle; padding: 0;"">
                            <img src=""https://upload.wikimedia.org/wikipedia/commons/thumb/e/e1/Google_Pay_Logo_%282020%29.svg/512px-Google_Pay_Logo_%282020%29.svg.png"" alt=""Google Pay"" style=""height: 24px; display: block; border: 0;"" />
                        </td>
                        <td align=""right"" style=""vertical-align: middle; padding: 0; font-size: 16px; color: #4a3f6b; font-weight: 800; font-family: 'Outfit', 'Inter', 'Segoe UI', sans-serif;"">
                            <span style=""font-weight: 600; color: #5b5280; font-size: 14px; margin-right: 8px;"">GPay Number:</span> 9940839866
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

                try
                {
                    if (!string.IsNullOrWhiteSpace(member.Email))
                    {
                        await _emailService.SendEmailAsync(member.Email, $"New Event Notification: {eventItem.EventName}", emailBody, cancellationToken);
                        _logger.LogInformation("Successfully sent/logged event creation email to participant: {Email}", member.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send event creation email to participant: {Email}", member.Email);
                }
            });

            await Task.WhenAll(emailTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run email sending tasks for new event: {EventId}", eventItem.EventId);
        }

        return await GetByIdAsync(eventItem.EventId, cancellationToken);
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
        var memberAmounts = members.ToDictionary(
            member => member.MemberId,
            member =>
            {
                var baseAmount = overrideLookup.TryGetValue(member.MemberId, out var amount)
                    ? amount
                    : request.BaseAmount;

                // Apply 50% reduction for members with less than 1 year of tenure
                if (member.JoiningDate.AddYears(1) > eventItem.EventDate)
                {
                    baseAmount *= 0.5m;
                }

                return baseAmount;
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
}
