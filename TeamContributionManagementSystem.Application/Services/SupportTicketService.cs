using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
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

    public SupportTicketService(
        ILogger<SupportTicketService> logger,
        ISupportTicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null,
        ITicketTypeRepository? ticketTypeRepository = null,
        IPriorityRepository? priorityRepository = null)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _priorityRepository = priorityRepository;
    }

    public async Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _ticketRepository.GetAllAsync(status, ticketType, priority, cancellationToken);
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
            var all = await _ticketRepository.GetAllAsync(cancellationToken: cancellationToken);
            var nextNumber = all.Count + 1;
            var ticketNo = $"{CommonConstants.Defaults.TicketPrefix}-{DateTime.UtcNow.Year}-{nextNumber:D3}";

            var ticketType = request.TicketType?.Trim();
            if (string.IsNullOrWhiteSpace(ticketType) && _ticketTypeRepository != null)
            {
                var dbTicketTypes = await _ticketTypeRepository.GetAllAsync(true, cancellationToken);
                ticketType = dbTicketTypes.FirstOrDefault()?.TypeName ?? string.Empty;
            }

            var status = request.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status) && _statusRepository != null)
            {
                var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                status = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Open", StringComparison.OrdinalIgnoreCase))?.StatusName
                    ?? dbStatuses.FirstOrDefault()?.StatusName
                    ?? "Open";
            }
            else if (string.IsNullOrWhiteSpace(status))
            {
                status = "Open";
            }

            var priority = request.Priority?.Trim();
            if (string.IsNullOrWhiteSpace(priority) && _priorityRepository != null)
            {
                var dbPriorities = await _priorityRepository.GetAllAsync(true, cancellationToken);
                priority = dbPriorities.FirstOrDefault(p => p.PriorityName.Equals("Medium", StringComparison.OrdinalIgnoreCase))?.PriorityName
                    ?? dbPriorities.FirstOrDefault()?.PriorityName
                    ?? "Medium";
            }
            else if (string.IsNullOrWhiteSpace(priority))
            {
                priority = "Medium";
            }

            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid(),
                TicketNo = ticketNo,
                MemberName = request.MemberName.Trim(),
                MemberId = request.MemberId?.Trim(),
                RelatedEvent = request.RelatedEvent?.Trim(),
                TicketType = ticketType ?? string.Empty,
                Subject = request.Subject.Trim(),
                Description = request.Description.Trim(),
                Status = status,
                Priority = priority,
                AssignedTo = request.AssignedTo?.Trim(),
                RefNo = request.RefNo?.Trim() ?? $"REF-{DateTime.UtcNow:yyyyMMdd}-{nextNumber:D3}",
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
            }

            if (request.MemberId != null)
            {
                ticket.MemberId = string.IsNullOrWhiteSpace(request.MemberId) ? null : request.MemberId.Trim();
            }

            if (request.RelatedEvent != null)
            {
                ticket.RelatedEvent = string.IsNullOrWhiteSpace(request.RelatedEvent) ? null : request.RelatedEvent.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.TicketType))
            {
                ticket.TicketType = request.TicketType.Trim();
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
            }

            if (!string.IsNullOrWhiteSpace(request.Priority))
            {
                ticket.Priority = request.Priority.Trim();
            }

            if (request.AssignedTo != null)
            {
                ticket.AssignedTo = string.IsNullOrWhiteSpace(request.AssignedTo) ? null : request.AssignedTo.Trim();
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

