using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SupportTicketService(ISupportTicketRepository ticketRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<SupportTicketDto>> GetAllAsync(string? status = null, string? ticketType = null, string? priority = null, CancellationToken cancellationToken = default)
    {
        var tickets = await _ticketRepository.GetAllAsync(status, ticketType, priority, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<SupportTicketDto>>(tickets);
    }

    public async Task<SupportTicketDto> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");
        return _mapper.Map<SupportTicketDto>(ticket);
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
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
            AssignedTo = "Admin",
            RefNo = request.RefNo?.Trim() ?? $"REF-{DateTime.UtcNow:yyyyMMdd}-{nextNumber:D3}",
            Utr = request.Utr?.Trim(),
            Attachment = request.Attachment?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? "System" : user,
            CreatedOn = DateTime.UtcNow
        };

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupportTicketDto>(ticket);
    }

    public async Task<SupportTicketDto> UpdateAsync(Guid ticketId, UpdateSupportTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
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

        ticket.ModifiedBy = string.IsNullOrWhiteSpace(user) ? "System" : user;
        ticket.ModifiedOn = DateTime.UtcNow;

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupportTicketDto>(ticket);
    }

    public async Task<SupportTicketDto> ReplyAsync(Guid ticketId, ReplyTicketRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");

        var timestamp = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt");
        var sender = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
        var newNote = $"[{timestamp}] {sender}: {request.Message.Trim()}";

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
        }

        ticket.ModifiedBy = sender;
        ticket.ModifiedOn = DateTime.UtcNow;

        _ticketRepository.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupportTicketDto>(ticket);
    }

    public async Task DeleteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new KeyNotFoundException($"Support ticket with ID {ticketId} not found.");

        _ticketRepository.Delete(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
