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
}
