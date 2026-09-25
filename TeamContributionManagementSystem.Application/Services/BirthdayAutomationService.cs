using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class BirthdayAutomationService : IBirthdayAutomationService
{
    private readonly Microsoft.Extensions.Logging.ILogger<BirthdayAutomationService> _logger;
    private readonly IMemberRepository _memberRepository;
    private readonly IEventTypeRepository _eventTypeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventService _eventService;
    private readonly IEventRepository _eventRepository;

    public BirthdayAutomationService(Microsoft.Extensions.Logging.ILogger<BirthdayAutomationService> logger, 
        IMemberRepository memberRepository,
        IEventTypeRepository eventTypeRepository,
        IUserRepository userRepository,
        IEventService eventService,
        IEventRepository eventRepository)
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _eventTypeRepository = eventTypeRepository;
        _userRepository = userRepository;
        _eventService = eventService;
        _eventRepository = eventRepository;
    }

    public async Task<int> CreateMonthlyBirthdayEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var allEventTypes = await _eventTypeRepository.GetAllAsync(cancellationToken);
            var birthdayEventType = allEventTypes.FirstOrDefault(x => x.EventTypeName.Contains("Birthday", StringComparison.OrdinalIgnoreCase))
                ?? allEventTypes.FirstOrDefault()
                ?? throw new KeyNotFoundException("No active event type is configured in the database.");

            var adminUser = await _userRepository.GetFirstAdminAsync(cancellationToken)
                ?? throw new KeyNotFoundException("No admin user available for scheduled event creation.");

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
                EventName = $"Birthday Celebration - {member.Name}",
                EventTypeId = birthdayEventType.EventTypeId,
                EventDate = eventDate,
                Description = $"Auto-generated birthday contribution event for {member.Name}. birthday-member:{member.MemberId}",
                Status = EventStatus.Planned,
                ParticipantIds = participants,
                BaseAmount = birthdayMembers.Count * birthdayEventType.BaseAmount
            }, cancellationToken);

            createdCount++;
        }

        return createdCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateMonthlyBirthdayEventsAsync");
            throw;
        }
    }
}
