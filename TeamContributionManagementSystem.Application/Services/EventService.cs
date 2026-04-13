using AutoMapper;
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

    public EventService(
        IEventRepository eventRepository,
        IEventTypeRepository eventTypeRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository,
        IContributionRepository contributionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _eventTypeRepository = eventTypeRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
            Status = request.Status
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

        return await GetByIdAsync(eventItem.EventId, cancellationToken);
    }
    
    public async Task<EventDetailsDto> UpdateAsync(Guid eventId, CreateEventRequestDto request, CancellationToken cancellationToken = default)
    {
        var eventItem = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new KeyNotFoundException("Event not found.");

        var eventType = await _eventTypeRepository.GetByIdAsync(request.EventTypeId, cancellationToken)
            ?? throw new KeyNotFoundException("Event type not found.");

        eventItem.EventName = request.EventName.Trim();
        eventItem.EventTypeId = eventType.EventTypeId;
        eventItem.EventDate = request.EventDate.Date;
        eventItem.Description = request.Description.Trim();
        eventItem.Status = request.Status;

        _eventRepository.Update(eventItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(eventItem.EventId, cancellationToken);
    }
}
