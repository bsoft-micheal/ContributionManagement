using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
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
        IEventRepository? eventRepository = null)
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
                    (myUserEmail != null && string.Equals(t.CreatedBy, myUserEmail, StringComparison.OrdinalIgnoreCase))
                ).ToList();
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
                    (myUserEmail != null && string.Equals(ticket.CreatedBy, myUserEmail, StringComparison.OrdinalIgnoreCase));

                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Access denied to requested support ticket.");
                }
            }

            return _mapper.Map<SupportTicketDto>(ticket);
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
            var ticketNo = await _ticketRepository.GenerateNextTicketNoAsync(cancellationToken);

            var ticketType = request.TicketType?.Trim();
            Guid? ticketTypeId = null;
            if (_ticketTypeRepository != null)
            {
                if (string.IsNullOrWhiteSpace(ticketType))
                {
                    var dbTicketTypes = await _ticketTypeRepository.GetAllAsync(true, cancellationToken);
                    var firstType = dbTicketTypes.FirstOrDefault();
                    ticketType = firstType?.TypeName ?? string.Empty;
                    ticketTypeId = firstType?.TicketTypeId;
                }
                else
                {
                    var matched = await _ticketTypeRepository.GetByNameAsync(ticketType, cancellationToken);
                    ticketTypeId = matched?.TicketTypeId;
                }
            }

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

            Guid? eventId = null;
            if (!string.IsNullOrWhiteSpace(request.RelatedEvent) && _eventRepository != null)
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
                MemberName = request.MemberName.Trim(),
                MemberId = request.MemberId?.Trim(),
                RelatedEvent = request.RelatedEvent?.Trim(),
                TicketType = ticketType ?? string.Empty,
                Subject = string.Empty,
                Description = request.Description.Trim(),
                Status = status,
                Priority = priority,
                AssignedTo = null,
                RefNo = refNo,
                Utr = request.Utr?.Trim(),
                Attachment = request.Attachment?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketCreated, ticketNo, request.MemberName);

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
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

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

            if (request.RelatedEvent != null)
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
                ticket.ResolutionNotes = request.ResolutionNotes.Trim();
            }

            ticket.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            ticket.ModifiedOn = DateTime.UtcNow;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketUpdated, ticket.TicketNo, ticket.Status);

            return _mapper.Map<SupportTicketDto>(ticket);
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
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            var timestamp = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt");
            var sender = string.IsNullOrWhiteSpace(user) ? (string.IsNullOrWhiteSpace(ticket.MemberName) ? "User" : ticket.MemberName) : user.Trim();
            var newNote = $"[{timestamp}] {sender}: {request.Message.Trim()}";

            if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
            {
                ticket.ResolutionNotes = newNote;
            }
            else
            {
                ticket.ResolutionNotes += "\n            " + newNote;
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

            ticket.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            ticket.ModifiedOn = DateTime.UtcNow;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<SupportTicketDto>(ticket);
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
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.SupportTickets.NotFound);

            _ticketRepository.Delete(ticket);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.SupportTickets.TicketDeleted, ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}

