using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Exceptions;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly ILogger<SupportTicketService> _logger;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IStatusRepository? _statusRepository;
    private readonly ITicketTypeRepository? _ticketTypeRepository;
    private readonly IPriorityRepository? _priorityRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IMemberRepository? _memberRepository;
    private readonly IEventRepository? _eventRepository;
    private readonly IEmailService? _emailService;
    private readonly IUserRepository? _userRepository;
    private readonly IRoleRepository? _roleRepository;
    private readonly IConfiguration? _configuration;

    public SupportTicketService(
        ILogger<SupportTicketService> logger,
        ISupportTicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null,
        ITicketTypeRepository? ticketTypeRepository = null,
        IPriorityRepository? priorityRepository = null,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null,
        IEventRepository? eventRepository = null,
        IEmailService? emailService = null,
        IUserRepository? userRepository = null,
        IRoleRepository? roleRepository = null,
        IConfiguration? configuration = null)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _priorityRepository = priorityRepository;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _configuration = configuration;
    }

    public async Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _ticketRepository.GetAllAsync(status, ticketType, priority, cancellationToken);
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                var myMemberIdStr = myMember?.MemberId.ToString();
                var myMemberName = myMember?.Name.Trim();
                var myUserEmail = userEmail?.Trim();

                return all.Where(t =>
                    (myMemberIdStr != null && string.Equals(t.MemberId, myMemberIdStr, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(t.MemberName, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(t.CreatedBy, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (myUserEmail != null && string.Equals(t.CreatedBy, myUserEmail, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberIdStr != null && string.Equals(t.CreatedBy, myMemberIdStr, StringComparison.OrdinalIgnoreCase))
                ).ToList();
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

                if (orgUserId.HasValue && _eventRepository != null)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, cancellationToken: cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName?.Trim().ToLower() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var orgUserIdStr = orgUserId.Value.ToString();
                    var orgUserName = _currentUserService.UserName?.Trim();
                    var orgEmail = _currentUserService.Email?.Trim();

                    return all.Where(t =>
                        (t.EventId.HasValue && myEventIds.Contains(t.EventId.Value)) ||
                        (!string.IsNullOrWhiteSpace(t.RelatedEvent) && myEventNames.Contains(t.RelatedEvent.Trim().ToLower())) ||
                        (orgUserIdStr != null && string.Equals(t.MemberId, orgUserIdStr, StringComparison.OrdinalIgnoreCase)) ||
                        (orgUserName != null && string.Equals(t.CreatedBy, orgUserName, StringComparison.OrdinalIgnoreCase)) ||
                        (orgEmail != null && string.Equals(t.CreatedBy, orgEmail, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
            }
            return all;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                var myMemberIdStr = myMember?.MemberId.ToString();
                var myMemberName = myMember?.Name.Trim();
                var myUserEmail = userEmail?.Trim();

                bool isOwner =
                    (myMemberIdStr != null && string.Equals(ticket.MemberId, myMemberIdStr, StringComparison.OrdinalIgnoreCase)) ||
                    (myMemberName != null && string.Equals(ticket.MemberName, myMemberName, StringComparison.OrdinalIgnoreCase)) ||
                    (ticket.UserId.HasValue && myMember != null && ticket.UserId.Value == myMember.MemberId) ||
                    (ticket.CreatedBy.HasValue && myMember != null && ticket.CreatedBy.Value == myMember.MemberId) ||
                    (ticket.CreatedBy.HasValue && !string.IsNullOrWhiteSpace(myMemberIdStr) && ticket.CreatedBy.Value.ToString().Equals(myMemberIdStr, StringComparison.OrdinalIgnoreCase));

                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Access denied to requested support ticket.");
                }
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

                if (orgUserId.HasValue && _eventRepository != null)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgUserId.Value, cancellationToken: cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var orgUserIdStr = orgUserId.Value.ToString();
                    var orgUserName = _currentUserService.UserName?.Trim();

                    bool isAllowed =
                        (ticket.EventId.HasValue && myEventIds.Contains(ticket.EventId.Value)) ||
                        (!string.IsNullOrWhiteSpace(ticket.RelatedEvent) && myEventNames.Contains(ticket.RelatedEvent.Trim().ToLower())) ||
                        (orgUserIdStr != null && string.Equals(ticket.MemberId, orgUserIdStr, StringComparison.OrdinalIgnoreCase)) ||
                        (ticket.UserId.HasValue && ticket.UserId.Value == orgUserId.Value) ||
                        (ticket.CreatedBy.HasValue && ticket.CreatedBy.Value == orgUserId.Value) ||
                        (orgUserName != null && string.Equals(ticket.MemberName, orgUserName, StringComparison.OrdinalIgnoreCase));

                    if (!isAllowed)
                    {
                        throw new UnauthorizedAccessException("Access denied to requested support ticket.");
                    }
                }
            }

            var dto = _mapper.Map<SupportTicketDto>(ticket);
            dto.ResolutionNotesHistory = await _ticketRepository.GetResolutionNotesByTicketIdAsync(ticket.TicketId, cancellationToken);
            dto.VerifiedBy = !string.IsNullOrWhiteSpace(ticket.AssignedTo) ? ticket.AssignedTo : null;
            if (ticket.ModifiedBy.HasValue && _userRepository != null)
            {
                var modUser = await _userRepository.GetByIdAsync(ticket.ModifiedBy.Value, cancellationToken);
                if (modUser != null)
                {
                    var modName = !string.IsNullOrWhiteSpace(modUser.FullName) ? modUser.FullName : modUser.Username;
                    dto.ModifiedBy = modName;
                    if (string.IsNullOrWhiteSpace(dto.VerifiedBy))
                    {
                        dto.VerifiedBy = modName;
                    }
                }
            }
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticketType = request.TicketType?.Trim();
            Guid? ticketTypeId = request.TicketTypeId;
            if (_ticketTypeRepository != null)
            {
                if (ticketTypeId.HasValue)
                {
                    var matchedById = await _ticketTypeRepository.GetByIdAsync(ticketTypeId.Value, cancellationToken);
                    if (matchedById != null)
                    {
                        ticketType = matchedById.TypeName;
                    }
                }
                else if (string.IsNullOrWhiteSpace(ticketType))
                {
                    var dbTicketTypes = await _ticketTypeRepository.GetAllAsync(true, cancellationToken);
                    var firstType = dbTicketTypes.FirstOrDefault(t => t.IsActive) ?? dbTicketTypes.FirstOrDefault();
                    ticketType = firstType?.TypeName ?? string.Empty;
                    ticketTypeId = firstType?.TicketTypeId;
                }
                else
                {
                    var matched = await _ticketTypeRepository.GetByNameAsync(ticketType, cancellationToken);
                    if (matched != null)
                    {
                        ticketTypeId = matched.TicketTypeId;
                        ticketType = matched.TypeName;
                    }
                }
            }

            Guid? eventId = request.EventId;
            string? relatedEvent = request.RelatedEvent?.Trim();
            if (!eventId.HasValue && !string.IsNullOrWhiteSpace(relatedEvent) && _eventRepository != null)
            {
                var eventObj = await _eventRepository.GetByNameAsync(relatedEvent, cancellationToken);
                eventId = eventObj?.EventId;
                if (eventObj != null)
                {
                    relatedEvent = eventObj.EventName;
                }
            }
            else if (eventId.HasValue && string.IsNullOrWhiteSpace(relatedEvent) && _eventRepository != null)
            {
                var eventObj = await _eventRepository.GetByIdAsync(eventId.Value, cancellationToken);
                if (eventObj != null)
                {
                    relatedEvent = eventObj.EventName;
                }
            }

            var existingTickets = await _ticketRepository.GetAllAsync(cancellationToken: cancellationToken);

            var reqMemberName = request.MemberName?.Trim();
            var reqMemberId = request.MemberId?.Trim();

            // Business rule: For each event and each ticket type, only one unresolved support ticket can exist until resolved
            var unverifiedTicketForType = existingTickets.FirstOrDefault(t =>
            {
                if (CommonConstants.TicketStatuses.IsVerified(t.Status))
                {
                    return false;
                }

                // Check event match (by EventId or RelatedEvent)
                bool matchesEvent = (eventId.HasValue && t.EventId.HasValue && t.EventId.Value == eventId.Value) ||
                    (!string.IsNullOrWhiteSpace(relatedEvent) && !string.IsNullOrWhiteSpace(t.RelatedEvent) &&
                     string.Equals(t.RelatedEvent.Trim(), relatedEvent, StringComparison.OrdinalIgnoreCase));

                if (!matchesEvent)
                {
                    return false;
                }

                // Check ticket type match
                bool matchesTicketType =
                    (!string.IsNullOrWhiteSpace(ticketType) && string.Equals(t.TicketType?.Trim(), ticketType, StringComparison.OrdinalIgnoreCase)) ||
                    (ticketTypeId.HasValue && t.TicketTypeId.HasValue && t.TicketTypeId.Value == ticketTypeId.Value);

                if (!matchesTicketType)
                {
                    return false;
                }

                bool matchesName = !string.IsNullOrWhiteSpace(reqMemberName) &&
                    string.Equals(t.MemberName?.Trim(), reqMemberName, StringComparison.OrdinalIgnoreCase);

                bool matchesId = !string.IsNullOrWhiteSpace(reqMemberId) &&
                    string.Equals(t.MemberId?.Trim(), reqMemberId, StringComparison.OrdinalIgnoreCase);

                bool matchesCurrentMember = false;
                if (_currentUserService != null && _currentUserService.IsMemberRole)
                {
                    var curMemberIdStr = _currentUserService.MemberId?.ToString();
                    var curUserEmail = _currentUserService.Email?.Trim();
                    var curUserName = _currentUserService.UserName?.Trim();

                    matchesCurrentMember =
                        (!string.IsNullOrWhiteSpace(curMemberIdStr) && (string.Equals(t.MemberId?.Trim(), curMemberIdStr, StringComparison.OrdinalIgnoreCase) || string.Equals(t.CreatedBy?.Trim(), curMemberIdStr, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrWhiteSpace(curUserEmail) && string.Equals(t.CreatedBy?.Trim(), curUserEmail, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(curUserName) && string.Equals(t.MemberName?.Trim(), curUserName, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(reqMemberName) || !string.IsNullOrWhiteSpace(reqMemberId) || (_currentUserService != null && _currentUserService.IsMemberRole))
                {
                    if (!matchesName && !matchesId && !matchesCurrentMember)
                    {
                        return false;
                    }
                }

                return true;
            });

            if (unverifiedTicketForType != null)
            {
                var displayType = !string.IsNullOrWhiteSpace(ticketType) ? ticketType : unverifiedTicketForType.TicketType;
                var displayEvent = !string.IsNullOrWhiteSpace(relatedEvent) ? relatedEvent : unverifiedTicketForType.RelatedEvent ?? "this event";
                throw new InvalidOperationException(string.Format(
                    CommonValidationMessages.ActiveTicketForEventAndTypeExists,
                    unverifiedTicketForType.TicketNo,
                    displayType,
                    displayEvent));
            }

            if (!string.IsNullOrWhiteSpace(request.MemberName) && !string.IsNullOrWhiteSpace(request.Description))
            {
                var recentDup = existingTickets.FirstOrDefault(t =>
                    string.Equals(t.MemberName, request.MemberName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.Description, request.Description.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    t.CreatedOn.HasValue &&
                    Math.Abs((DateTime.UtcNow - t.CreatedOn.Value).TotalSeconds) < 5);

                if (recentDup != null)
                {
                    throw new AlreadyProcessedException("This action has already been completed.");
                }
            }

            var ticketNo = await _ticketRepository.GenerateNextTicketNoAsync(cancellationToken);

            var status = request.Status?.Trim();
            Guid? statusId = null;
            if (_statusRepository != null)
            {
                if (string.IsNullOrWhiteSpace(status))
                {
                    var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                    var matched = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Open", StringComparison.OrdinalIgnoreCase))
                        ?? dbStatuses.FirstOrDefault();
                    status = matched?.StatusName ?? "Open";
                    statusId = matched?.StatusId;
                }
                else
                {
                    var matched = await _statusRepository.GetByNameAsync(status, cancellationToken);
                    statusId = matched?.StatusId;
                }
            }
            else if (string.IsNullOrWhiteSpace(status))
            {
                status = "Open";
            }

            var priority = request.Priority?.Trim();
            Guid? priorityId = null;
            if (_priorityRepository != null)
            {
                if (string.IsNullOrWhiteSpace(priority))
                {
                    var dbPriorities = await _priorityRepository.GetAllAsync(true, cancellationToken);
                    var matched = dbPriorities.FirstOrDefault(p => p.PriorityName.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                        ?? dbPriorities.FirstOrDefault();
                    priority = matched?.PriorityName ?? "Medium";
                    priorityId = matched?.PriorityId;
                }
                else
                {
                    var matched = await _priorityRepository.GetByNameAsync(priority, cancellationToken);
                    priorityId = matched?.PriorityId;
                }
            }
            else if (string.IsNullOrWhiteSpace(priority))
            {
                priority = "Medium";
            }

            var refNo = request.RefNo?.Trim();
            if (string.IsNullOrWhiteSpace(refNo))
            {
                refNo = await _ticketRepository.GenerateNextRefNoAsync(cancellationToken);
            }

            Guid? userId = null;
            if (!string.IsNullOrWhiteSpace(request.MemberId) && Guid.TryParse(request.MemberId, out var parsedGuid))
            {
                userId = parsedGuid;
            }
            else if (!string.IsNullOrWhiteSpace(request.MemberName) && _memberRepository != null)
            {
                var userObj = await _memberRepository.GetUserByNameAsync(request.MemberName.Trim(), cancellationToken);
                userId = userObj?.UserId;
            }
            if (userId == null && _currentUserService != null)
            {
                if (_currentUserService.MemberId.HasValue)
                {
                    userId = _currentUserService.MemberId.Value;
                }
                else if (Guid.TryParse(_currentUserService.UserId, out var curUserGuid))
                {
                    userId = curUserGuid;
                }
            }

            if (!eventId.HasValue && !string.IsNullOrWhiteSpace(request.RelatedEvent) && _eventRepository != null)
            {
                var eventObj = await _eventRepository.GetByNameAsync(request.RelatedEvent.Trim(), cancellationToken);
                eventId = eventObj?.EventId;
            }

            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid(),
                TicketNo = ticketNo,
                UserId = userId,
                EventId = eventId,
                TicketTypeId = ticketTypeId,
                PriorityId = priorityId,
                StatusId = statusId,
                MemberName = request.MemberName?.Trim() ?? string.Empty,
                MemberId = request.MemberId?.Trim(),
                RelatedEvent = request.RelatedEvent?.Trim(),
                TicketType = ticketType ?? string.Empty,
                Subject = string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                Status = status,
                Priority = priority,
                AssignedTo = null,
                RefNo = refNo,
                Utr = request.Utr?.Trim(),
                Attachment = request.Attachment?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow
            };

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketCreated, ticketNo, request.MemberName);

            // Safe non-blocking background email notification dispatch to active organizers
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendSupportTicketEmailNotificationAsync(ticket, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background error sending support ticket email notification for TicketNo: {TicketNo}", ticket.TicketNo);
                }
            });

            return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
            if (ticket == null)
            {
                var existingAny = await _ticketRepository.GetByIdWithDeletedAsync(ticketId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            if (request.ModifiedOn.HasValue && ticket.ModifiedOn.HasValue)
            {
                if (Math.Abs((ticket.ModifiedOn.Value - request.ModifiedOn.Value).TotalSeconds) > 1)
                {
                    throw new RecordModifiedException();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.MemberName))
            {
                ticket.MemberName = request.MemberName.Trim();
                if (_memberRepository != null)
                {
                    var userObj = await _memberRepository.GetUserByNameAsync(ticket.MemberName, cancellationToken);
                    if (userObj != null) ticket.UserId = userObj.UserId;
                }
            }

            if (request.MemberId != null)
            {
                ticket.MemberId = string.IsNullOrWhiteSpace(request.MemberId) ? null : request.MemberId.Trim();
                if (Guid.TryParse(ticket.MemberId, out var parsedGuid))
                {
                    ticket.UserId = parsedGuid;
                }
            }

            if (request.EventId.HasValue)
            {
                ticket.EventId = request.EventId.Value;
                if (!string.IsNullOrWhiteSpace(request.RelatedEvent))
                {
                    ticket.RelatedEvent = request.RelatedEvent.Trim();
                }
            }
            else if (request.RelatedEvent != null)
            {
                ticket.RelatedEvent = string.IsNullOrWhiteSpace(request.RelatedEvent) ? null : request.RelatedEvent.Trim();
                if (!string.IsNullOrWhiteSpace(ticket.RelatedEvent) && _eventRepository != null)
                {
                    var eventObj = await _eventRepository.GetByNameAsync(ticket.RelatedEvent, cancellationToken);
                    ticket.EventId = eventObj?.EventId;
                }
                else if (string.IsNullOrWhiteSpace(ticket.RelatedEvent))
                {
                    ticket.EventId = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.TicketType))
            {
                ticket.TicketType = request.TicketType.Trim();
                if (_ticketTypeRepository != null)
                {
                    var typeObj = await _ticketTypeRepository.GetByNameAsync(ticket.TicketType, cancellationToken);
                    if (typeObj != null) ticket.TicketTypeId = typeObj.TicketTypeId;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Subject))
            {
                ticket.Subject = request.Subject.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                ticket.Description = request.Description.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                ticket.Status = request.Status.Trim();
                if (_statusRepository != null)
                {
                    var statusObj = await _statusRepository.GetByNameAsync(ticket.Status, cancellationToken);
                    if (statusObj != null) ticket.StatusId = statusObj.StatusId;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                ticket.Priority = request.Priority.Trim();
                if (_priorityRepository != null)
                {
                    var priorityObj = await _priorityRepository.GetByNameAsync(ticket.Priority, cancellationToken);
                    if (priorityObj != null) ticket.PriorityId = priorityObj.PriorityId;
                }
            }

            if (request.RefNo != null)
            {
                ticket.RefNo = string.IsNullOrWhiteSpace(request.RefNo) ? null : request.RefNo.Trim();
            }

            if (request.Utr != null)
            {
                ticket.Utr = string.IsNullOrWhiteSpace(request.Utr) ? null : request.Utr.Trim();
            }

            if (request.Attachment != null)
            {
                ticket.Attachment = string.IsNullOrWhiteSpace(request.Attachment) ? null : request.Attachment.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
            {
                var trimmedNote = request.ResolutionNotes.Trim();
                ticket.ResolutionNotes = trimmedNote;

                var modifierGuid = CommonMethods.ParseNullableGuid(user)
                    ?? (_currentUserService?.UserId != null && Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null);
                var modifierName = !string.IsNullOrWhiteSpace(_currentUserService?.UserName)
                    ? _currentUserService.UserName
                    : "Admin";

                await _ticketRepository.AddResolutionNoteAsync(new TicketResolutionNote
                {
                    NoteId = Guid.NewGuid(),
                    TicketId = ticket.TicketId,
                    UserId = modifierGuid,
                    SenderName = modifierName,
                    Notes = trimmedNote,
                    StatusId = ticket.StatusId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = modifierGuid
                }, cancellationToken);
            }

            ticket.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            ticket.ModifiedOn = DateTime.UtcNow;

            // Business rule: If ticket is unresolved, verify no other unresolved ticket exists for the same event and ticket type
            if (!CommonConstants.TicketStatuses.IsVerified(ticket.Status))
            {
                var allTickets = await _ticketRepository.GetAllAsync(cancellationToken: cancellationToken);
                var conflictingTicket = allTickets.FirstOrDefault(t =>
                {
                    // Exclude current ticket being edited
                    if (t.TicketId == ticketId || string.Equals(t.TicketNo, ticket.TicketNo, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (CommonConstants.TicketStatuses.IsVerified(t.Status))
                    {
                        return false;
                    }

                    // Check event match
                    bool matchesEvent = (ticket.EventId.HasValue && t.EventId.HasValue && t.EventId.Value == ticket.EventId.Value) ||
                        (!string.IsNullOrWhiteSpace(ticket.RelatedEvent) && !string.IsNullOrWhiteSpace(t.RelatedEvent) &&
                         string.Equals(t.RelatedEvent.Trim(), ticket.RelatedEvent.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!matchesEvent)
                    {
                        return false;
                    }

                    // Check ticket type match
                    bool matchesTicketType =
                        (!string.IsNullOrWhiteSpace(ticket.TicketType) && string.Equals(t.TicketType?.Trim(), ticket.TicketType.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                        (ticket.TicketTypeId.HasValue && t.TicketTypeId.HasValue && t.TicketTypeId.Value == ticket.TicketTypeId.Value);

                    if (!matchesTicketType)
                    {
                        return false;
                    }

                    // Check member/user match
                    var reqMemName = ticket.MemberName?.Trim();
                    var reqMemId = ticket.MemberId?.Trim();

                    bool matchesName = !string.IsNullOrWhiteSpace(reqMemName) &&
                        string.Equals(t.MemberName?.Trim(), reqMemName, StringComparison.OrdinalIgnoreCase);

                    bool matchesId = !string.IsNullOrWhiteSpace(reqMemId) &&
                        string.Equals(t.MemberId?.Trim(), reqMemId, StringComparison.OrdinalIgnoreCase);

                    bool matchesCurrentMember = false;
                    if (_currentUserService != null && _currentUserService.IsMemberRole)
                    {
                        var curMemberIdStr = _currentUserService.MemberId?.ToString();
                        var curUserEmail = _currentUserService.Email?.Trim();
                        var curUserName = _currentUserService.UserName?.Trim();

                        matchesCurrentMember =
                            (!string.IsNullOrWhiteSpace(curMemberIdStr) && (string.Equals(t.MemberId?.Trim(), curMemberIdStr, StringComparison.OrdinalIgnoreCase) || string.Equals(t.CreatedBy?.Trim(), curMemberIdStr, StringComparison.OrdinalIgnoreCase))) ||
                            (!string.IsNullOrWhiteSpace(curUserEmail) && string.Equals(t.CreatedBy?.Trim(), curUserEmail, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(curUserName) && string.Equals(t.MemberName?.Trim(), curUserName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (!string.IsNullOrWhiteSpace(reqMemName) || !string.IsNullOrWhiteSpace(reqMemId) || (_currentUserService != null && _currentUserService.IsMemberRole))
                    {
                        if (!matchesName && !matchesId && !matchesCurrentMember)
                        {
                            return false;
                        }
                    }

                    return true;
                });

                if (conflictingTicket != null)
                {
                    var displayType = !string.IsNullOrWhiteSpace(ticket.TicketType) ? ticket.TicketType : conflictingTicket.TicketType;
                    var displayEvent = !string.IsNullOrWhiteSpace(ticket.RelatedEvent) ? ticket.RelatedEvent : conflictingTicket.RelatedEvent ?? "this event";
                    throw new InvalidOperationException(string.Format(
                        CommonValidationMessages.ActiveTicketForEventAndTypeExists,
                        conflictingTicket.TicketNo,
                        displayType,
                        displayEvent));
                }
            }

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketUpdated, ticket.TicketNo, ticket.Status);
            var updatedDto = _mapper.Map<SupportTicketDto>(ticket);
            updatedDto.ResolutionNotesHistory = await _ticketRepository.GetResolutionNotesByTicketIdAsync(ticket.TicketId, cancellationToken);
            return updatedDto;
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

    public async Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
            if (ticket == null)
            {
                var existingAny = await _ticketRepository.GetByIdWithDeletedAsync(ticketId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            if (request.ModifiedOn.HasValue && ticket.ModifiedOn.HasValue)
            {
                if (Math.Abs((ticket.ModifiedOn.Value - request.ModifiedOn.Value).TotalSeconds) > 1)
                {
                    throw new RecordModifiedException();
                }
            }

            var sender = !string.IsNullOrWhiteSpace(user)
                ? user.Trim()
                : (!string.IsNullOrWhiteSpace(_currentUserService?.UserName)
                    ? _currentUserService.UserName
                    : (string.IsNullOrWhiteSpace(ticket.MemberName) ? "Admin" : ticket.MemberName));
            var newNote = request.Message.Trim();
            if (newNote.Length > 250)
            {
                throw new ArgumentException(CommonValidationMessages.ReplyMessageMaxLength);
            }

            ticket.AssignedTo = sender;

            if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
            {
                ticket.ResolutionNotes = newNote;
            }
            else
            {
                ticket.ResolutionNotes += "\n" + newNote;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                ticket.Status = request.Status.Trim();
                if (_statusRepository != null)
                {
                    var statusObj = await _statusRepository.GetByNameAsync(ticket.Status, cancellationToken);
                    if (statusObj != null) ticket.StatusId = statusObj.StatusId;
                }
            }

            var actorGuid = CommonMethods.ParseNullableGuid(user)
                ?? (_currentUserService?.UserId != null && Guid.TryParse(_currentUserService.UserId, out var uid) ? uid : null);

            ticket.ModifiedBy = actorGuid;
            ticket.ModifiedOn = DateTime.UtcNow;

            await _ticketRepository.AddResolutionNoteAsync(new TicketResolutionNote
            {
                NoteId = Guid.NewGuid(),
                TicketId = ticket.TicketId,
                UserId = actorGuid,
                SenderName = sender,
                Notes = newNote,
                StatusId = ticket.StatusId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorGuid
            }, cancellationToken);

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = _mapper.Map<SupportTicketDto>(ticket);
            resultDto.ResolutionNotesHistory = await _ticketRepository.GetResolutionNotesByTicketIdAsync(ticket.TicketId, cancellationToken);
            return resultDto;
        }
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(ReplyAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
            if (ticket == null)
            {
                var existingAny = await _ticketRepository.GetByIdWithDeletedAsync(ticketId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            _ticketRepository.Delete(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketDeleted, ticketId);
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

    private async Task SendSupportTicketEmailNotificationAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            if (_emailService == null)
            {
                _logger.LogWarning("IEmailService is not available. Skipping email notification for support ticket {TicketNo}.", ticket.TicketNo);
                return;
            }

            var recipientEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. ALL ACTIVE ADMINS: Admin can receive all the support ticket notifications
            try
            {
                Guid? adminRoleId = null;
                if (_roleRepository != null)
                {
                    var adminRole = await _roleRepository.GetByNameAsync(CommonConstants.UserRoles.Admin, cancellationToken);
                    adminRoleId = adminRole?.RoleId;
                }

                if (_userRepository != null)
                {
                    var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                    foreach (var u in allUsers)
                    {
                        if (u.IsActive && !u.IsDeleted && !string.IsNullOrWhiteSpace(u.Email))
                        {
                            bool isAdmin =
                                (adminRoleId.HasValue && (
                                    (u.RoleId.HasValue && u.RoleId.Value == adminRoleId.Value)
                                    || (u.RoleIds != null && u.RoleIds.Contains(adminRoleId.Value))
                                    || (u.PrimaryRoleIds != null && u.PrimaryRoleIds.Contains(adminRoleId.Value))
                                    || (u.SecondaryRoleIds != null && u.SecondaryRoleIds.Contains(adminRoleId.Value))
                                ))
                                || string.Equals(u.Role, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(u.RoleName, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
                                || (u.Roles != null && u.Roles.Any(r => string.Equals(r, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)))
                                || (u.PrimaryRoles != null && u.PrimaryRoles.Any(r => string.Equals(r, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)))
                                || (u.SecondaryRoles != null && u.SecondaryRoles.Any(r => string.Equals(r, CommonConstants.UserRoles.Admin, StringComparison.OrdinalIgnoreCase)));

                            if (isAdmin)
                            {
                                recipientEmails.Add(u.Email.Trim());
                                _logger.LogInformation("Added Admin {Email} to support ticket {TicketNo} notification recipients.", u.Email, ticket.TicketNo);
                            }
                        }
                    }

                    // Fallback to GetFirstAdminAsync if no admin users were loaded
                    if (recipientEmails.Count == 0)
                    {
                        var firstAdmin = await _userRepository.GetFirstAdminAsync(cancellationToken);
                        if (firstAdmin != null && firstAdmin.IsActive && !firstAdmin.IsDeleted && !string.IsNullOrWhiteSpace(firstAdmin.Email))
                        {
                            recipientEmails.Add(firstAdmin.Email.Trim());
                            _logger.LogInformation("Added First Admin {Email} to support ticket {TicketNo} notification recipients via fallback.", firstAdmin.Email, ticket.TicketNo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving Admin recipients for support ticket {TicketNo}.", ticket.TicketNo);
            }

            // 2. ORGANISER: Organiser can receive ONLY if he/she created the event
            if (ticket.EventId.HasValue || !string.IsNullOrWhiteSpace(ticket.RelatedEvent))
            {
                try
                {
                    Domain.Entities.Event? eventObj = null;
                    if (ticket.EventId.HasValue && ticket.EventId.Value != Guid.Empty && _eventRepository != null)
                    {
                        try
                        {
                            eventObj = await _eventRepository.GetByIdAsync(ticket.EventId.Value, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not resolve Event by EventId {EventId}.", ticket.EventId);
                        }
                    }

                    if (eventObj == null && !string.IsNullOrWhiteSpace(ticket.RelatedEvent) && _eventRepository != null)
                    {
                        try
                        {
                            eventObj = await _eventRepository.GetByNameAsync(ticket.RelatedEvent.Trim(), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not resolve Event by name '{RelatedEvent}'.", ticket.RelatedEvent);
                        }
                    }

                    if (eventObj != null)
                    {
                        if (string.IsNullOrWhiteSpace(ticket.RelatedEvent))
                        {
                            ticket.RelatedEvent = eventObj.EventName;
                        }

                        if (eventObj.CreatedBy != Guid.Empty)
                        {
                            string? creatorEmail = null;
                            if (_userRepository != null)
                            {
                                try
                                {
                                    var creatorUser = await _userRepository.GetByIdAsync(eventObj.CreatedBy, cancellationToken);
                                    if (creatorUser != null && creatorUser.IsActive && !creatorUser.IsDeleted && !string.IsNullOrWhiteSpace(creatorUser.Email))
                                    {
                                        creatorEmail = creatorUser.Email.Trim();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not query event creator user for Event '{EventName}'.", eventObj.EventName);
                                }
                            }

                            if (string.IsNullOrWhiteSpace(creatorEmail) && _memberRepository != null)
                            {
                                try
                                {
                                    var creatorMember = await _memberRepository.GetByIdAsync(eventObj.CreatedBy, cancellationToken);
                                    if (creatorMember != null && creatorMember.IsActive && !creatorMember.IsDeleted && !string.IsNullOrWhiteSpace(creatorMember.Email))
                                    {
                                        creatorEmail = creatorMember.Email.Trim();
                                    }
                                }
                                catch { }
                            }

                            if (string.IsNullOrWhiteSpace(creatorEmail) && eventObj.CreatedByUser != null && !string.IsNullOrWhiteSpace(eventObj.CreatedByUser.Email))
                            {
                                creatorEmail = eventObj.CreatedByUser.Email.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(creatorEmail))
                            {
                                recipientEmails.Add(creatorEmail);
                                _logger.LogInformation("Added Event Creator Organiser {Email} for Event '{EventName}' (Ticket {TicketNo}).", creatorEmail, eventObj.EventName, ticket.TicketNo);
                            }
                            else
                            {
                                _logger.LogWarning("Event '{EventName}' creator ID {CreatedBy} email could not be resolved for ticket {TicketNo}.", eventObj.EventName, eventObj.CreatedBy, ticket.TicketNo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolving event creator organiser for support ticket {TicketNo}.", ticket.TicketNo);
                }
            }

            if (recipientEmails.Count == 0)
            {
                _logger.LogWarning("No active Admin or Event Organiser found for support ticket {TicketNo} (RefNo: {RefNo}, EventId: {EventId}).", ticket.TicketNo, ticket.RefNo, ticket.EventId);
                return;
            }

            // 3. Resolve Member Contact Info to display inside the email body
            string? memberEmail = null;
            string? memberPhone = null;
            if (ticket.UserId.HasValue && _userRepository != null)
            {
                try
                {
                    var userObj = await _userRepository.GetByIdAsync(ticket.UserId.Value, cancellationToken);
                    if (userObj != null)
                    {
                        memberEmail = userObj.Email;
                        memberPhone = userObj.Phone;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(memberEmail) && ticket.UserId.HasValue && _memberRepository != null)
            {
                try
                {
                    var memObj = await _memberRepository.GetByIdAsync(ticket.UserId.Value, cancellationToken);
                    if (memObj != null)
                    {
                        memberEmail = memObj.Email;
                        memberPhone = memObj.Phone;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(memberEmail) && !string.IsNullOrWhiteSpace(ticket.MemberName) && _memberRepository != null)
            {
                try
                {
                    var memObj = await _memberRepository.GetUserByNameAsync(ticket.MemberName.Trim(), cancellationToken);
                    if (memObj != null)
                    {
                        memberEmail = memObj.Email;
                        memberPhone = memObj.Phone;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(memberEmail) && _currentUserService != null)
            {
                memberEmail = _currentUserService.Email;
            }

            // 4. Build Email Subject and Body
            var subjectTitle = !string.IsNullOrWhiteSpace(ticket.Subject) ? ticket.Subject : (!string.IsNullOrWhiteSpace(ticket.TicketType) ? ticket.TicketType : "Support Request");
            var subject = $"[Support Ticket #{ticket.TicketNo}] New Support Request: {subjectTitle}";
            var htmlBody = BuildSupportTicketEmailHtml(ticket, memberEmail, memberPhone);

            // 5. Send Email to Recipients safely (Admin and Event Organiser)
            foreach (var recipientEmail in recipientEmails)
            {
                try
                {
                    await _emailService.SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken: cancellationToken);
                    _logger.LogInformation("Support ticket email notification sent successfully. TicketNo: {TicketNo}, Recipient: {RecipientEmail}", ticket.TicketNo, recipientEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send support ticket email notification. TicketNo: {TicketNo}, Recipient: {RecipientEmail}", ticket.TicketNo, recipientEmail);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process support ticket email notification. TicketNo: {TicketNo}", ticket.TicketNo);
        }
    }

    private string BuildSupportTicketEmailHtml(SupportTicket ticket, string? memberEmail, string? memberPhone)
    {
        var frontendBaseUrl = _configuration?["Cors:AllowedOrigins:0"]
            ?? _configuration?["FrontendUrl"]
            ?? CommonConstants.Defaults.DefaultFrontendUrl;

        var viewTicketUrl = $"{frontendBaseUrl.TrimEnd('/')}/support-tickets";

        var priorityLower = (ticket.Priority ?? "medium").ToLowerInvariant();
        var badgeClass = priorityLower.Contains("urgent") ? "urgent" : (priorityLower.Contains("high") ? "high" : (priorityLower.Contains("low") ? "low" : "medium"));

        var relatedEventRow = !string.IsNullOrWhiteSpace(ticket.RelatedEvent)
            ? $"<tr><td class=\"label\">Related Event:</td><td class=\"value\">{WebUtility.HtmlEncode(ticket.RelatedEvent)}</td></tr>"
            : string.Empty;

        var memberEmailRow = !string.IsNullOrWhiteSpace(memberEmail)
            ? $"<tr><td class=\"label\">Email:</td><td class=\"value\">{WebUtility.HtmlEncode(memberEmail)}</td></tr>"
            : string.Empty;

        var memberPhoneRow = !string.IsNullOrWhiteSpace(memberPhone)
            ? $"<tr><td class=\"label\">Phone:</td><td class=\"value\">{WebUtility.HtmlEncode(memberPhone)}</td></tr>"
            : string.Empty;

        var createdAtText = (ticket.CreatedAt ?? DateTime.UtcNow).ToString("dd MMMM yyyy, hh:mm tt");

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333333; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05); }}
        .header {{ background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); color: #ffffff; padding: 25px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 22px; font-weight: 600; }}
        .header p {{ margin: 5px 0 0 0; font-size: 14px; opacity: 0.9; }}
        .content {{ padding: 30px; }}
        .section-title {{ font-size: 16px; font-weight: 700; color: #1e3c72; border-bottom: 2px solid #eef2f5; padding-bottom: 8px; margin-top: 20px; margin-bottom: 15px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; }}
        .info-table td {{ padding: 8px 12px; font-size: 14px; vertical-align: top; }}
        .info-table td.label {{ font-weight: 600; color: #666666; width: 35%; background-color: #fafbfc; border-radius: 4px; }}
        .info-table td.value {{ color: #222222; }}
        .badge {{ display: inline-block; padding: 4px 10px; font-size: 12px; font-weight: 600; border-radius: 12px; }}
        .badge-urgent {{ background-color: #ffebee; color: #c62828; }}
        .badge-high {{ background-color: #fff3e0; color: #e65100; }}
        .badge-medium {{ background-color: #e3f2fd; color: #1565c0; }}
        .badge-low {{ background-color: #f1f8e9; color: #33691e; }}
        .description-box {{ background-color: #f8f9fa; border-left: 4px solid #2a5298; padding: 12px 15px; margin-top: 5px; border-radius: 0 4px 4px 0; font-size: 14px; line-height: 1.5; }}
        .btn-container {{ text-align: center; margin-top: 30px; margin-bottom: 10px; }}
        .btn {{ display: inline-block; background-color: #2a5298; color: #ffffff !important; text-decoration: none; padding: 12px 28px; font-size: 15px; font-weight: 600; border-radius: 6px; box-shadow: 0 2px 5px rgba(0,0,0,0.15); }}
        .footer {{ background-color: #f8f9fa; text-align: center; padding: 15px; font-size: 12px; color: #888888; border-top: 1px solid #eeeeee; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Support Ticket Submitted</h1>
            <p>Team Contribution Management System</p>
        </div>
        <div class=""content"">
            <p style=""font-size: 15px; margin-top: 0;"">Hello Admin / Organizer,</p>
            <p style=""font-size: 14px; color: #555555;"">A new support ticket has been submitted by a member and requires your attention.</p>
            
            <div class=""section-title"">Ticket Details</div>
            <table class=""info-table"">
                <tr>
                    <td class=""label"">Ticket No:</td>
                    <td class=""value""><strong>{WebUtility.HtmlEncode(ticket.TicketNo)}</strong></td>
                </tr>
                <tr>
                    <td class=""label"">Reference No:</td>
                    <td class=""value"">{WebUtility.HtmlEncode(ticket.RefNo ?? string.Empty)}</td>
                </tr>
                <tr>
                    <td class=""label"">Ticket Type:</td>
                    <td class=""value"">{WebUtility.HtmlEncode(ticket.TicketType)}</td>
                </tr>
                <tr>
                    <td class=""label"">Priority:</td>
                    <td class=""value""><span class=""badge badge-{badgeClass}"">{WebUtility.HtmlEncode(ticket.Priority)}</span></td>
                </tr>
                {relatedEventRow}
                <tr>
                    <td class=""label"">Created At:</td>
                    <td class=""value"">{createdAtText}</td>
                </tr>
            </table>

            <div class=""section-title"">Description</div>
            <div class=""description-box"">{WebUtility.HtmlEncode(ticket.Description)}</div>

            <div class=""section-title"">Member Details</div>
            <table class=""info-table"">
                <tr>
                    <td class=""label"">Name:</td>
                    <td class=""value""><strong>{WebUtility.HtmlEncode(ticket.MemberName)}</strong></td>
                </tr>
                {memberEmailRow}
                {memberPhoneRow}
            </table>

            <div class=""btn-container"">
                <a href=""{viewTicketUrl}"" class=""btn"" target=""_blank"">View Support Ticket</a>
            </div>
        </div>
        <div class=""footer"">
            This is an automated notification from Team Contribution Management System.
        </div>
    </div>
</body>
</html>";
    }
}

