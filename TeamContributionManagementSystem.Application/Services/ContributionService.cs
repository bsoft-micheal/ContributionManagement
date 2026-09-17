using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class ContributionService : IContributionService
{
    private readonly IContributionRepository _contributionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContributionService(IContributionRepository contributionRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var contributions = await _contributionRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<ContributionDto>>(contributions);
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var contributions = await _contributionRepository.GetByEventIdAsync(eventId, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<ContributionDto>>(contributions);
    }

    public async Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default)
    {
        var contribution = await _contributionRepository.GetByEventAndMemberAsync(request.EventId, request.MemberId, cancellationToken)
            ?? throw new KeyNotFoundException("Contribution record not found.");

        if (request.Amount.HasValue)
        {
            contribution.Amount = request.Amount.Value;
        }

        contribution.PaymentStatus = PaymentStatus.Paid;
        contribution.PaymentDate = request.PaymentDate?.ToUniversalTime() ?? DateTime.UtcNow;
        contribution.PaymentMode = request.PaymentMode;

        _contributionRepository.Update(contribution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ContributionDto>(contribution);
    }

    public async Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        var contributions = await _contributionRepository.GetByMemberEmailAsync(userEmail, cancellationToken);

        if (contributions.Count == 0)
        {
            return new MemberContributionSummaryDto
            {
                MemberName = string.Empty,
                TotalPaidAmount = 0,
                TotalPendingAmount = 0
            };
        }

        var member = contributions.First().Member!;
        var paidContributions = contributions.Where(c => c.PaymentStatus == PaymentStatus.Paid).ToList();

        var categoryBreakdown = paidContributions
            .GroupBy(c => c.Event?.EventType?.EventTypeName ?? "Uncategorized")
            .Select(g => new ContributionCategoryBreakdownDto
            {
                CategoryName = g.Key,
                TotalPaid = g.Sum(c => c.Amount),
                EventCount = g.Select(c => c.EventId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalPaid)
            .ToList();

        var eventBreakdown = contributions
            .Select(c => new ContributionEventBreakdownDto
            {
                EventName = c.Event?.EventName ?? "Unknown Event",
                CategoryName = c.Event?.EventType?.EventTypeName ?? "Uncategorized",
                Amount = c.Amount,
                PaymentStatus = c.PaymentStatus.ToString(),
                PaymentDate = c.PaymentDate
            })
            .ToList();

        return new MemberContributionSummaryDto
        {
            MemberId = member.MemberId,
            MemberName = member.Name,
            TotalPaidAmount = paidContributions.Sum(c => c.Amount),
            TotalPendingAmount = contributions.Where(c => c.PaymentStatus != PaymentStatus.Paid).Sum(c => c.Amount),
            CategoryBreakdown = categoryBreakdown,
            EventBreakdown = eventBreakdown
        };
    }
}
