using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly Microsoft.Extensions.Logging.ILogger<SupportTicketService> _logger;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SupportTicketService(Microsoft.Extensions.Logging.ILogger<SupportTicketService> logger, ISupportTicketRepository ticketRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var tickets = await _ticketRepository.GetAllAsync(status, ticketType, priority, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<SupportTicketDto>>(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");
        return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _ticketRepository.GetAllAsync(cancellationToken: cancellationToken);
        var nextNumber = all.Count + 1;
        var ticketNo = $"TKT-{DateTime.UtcNow.Year}-{nextNumber:D3}";

        var ticket = new SupportTicket
        {
            TicketId = Guid.NewGuid(),
            TicketNo = ticketNo,
            MemberName = request.MemberName.Trim(),
            MemberId = request.MemberId?.Trim(),
            RelatedEvent = request.RelatedEvent?.Trim(),
            TicketType = request.TicketType.Trim(),
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Status = "Open",
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority.Trim(),
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

        return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            ticket.Status = request.Status.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            ticket.Priority = request.Priority.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.AssignedTo))
        {
            ticket.AssignedTo = request.AssignedTo.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
        {
            ticket.ResolutionNotes = request.ResolutionNotes.Trim();
        }

        ticket.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
        ticket.ModifiedOn = DateTime.UtcNow;

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupportTicketDto>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    public async Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");

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
            _logger.LogError(ex, "Error in ReplyAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");

        _ticketRepository.Delete(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}
