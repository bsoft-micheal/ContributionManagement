using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class ContributionService : IContributionService
{
    private readonly ILogger<ContributionService> _logger;
    private readonly IContributionRepository _contributionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContributionService(ILogger<ContributionService> logger, IContributionRepository contributionRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _contributionRepository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _contributionRepository.GetByEventIdAsync(eventId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEventIdAsync));
            throw;
        }
    }

    public async Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var contribution = await _contributionRepository.GetByEventAndMemberAsync(request.EventId, request.MemberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Contributions.NotFound);


            var targetAmount = request.Amount ?? contribution.Amount;

            if (request.PaymentMode == PaymentMode.Split)
            {
                if (!request.CashAmount.HasValue || !request.UpiAmount.HasValue)
                {
                    throw new ArgumentException(CommonMessages.Contributions.SplitPaymentAmountsRequired);
                }

                if (request.CashAmount.Value < 0 || request.UpiAmount.Value < 0)
                {
                    throw new ArgumentException(CommonMessages.Contributions.SplitPaymentNegative);
                }

                var splitSum = Math.Round(request.CashAmount.Value + request.UpiAmount.Value, 2);
                if (splitSum != Math.Round(targetAmount, 2))
                {
                    throw new ArgumentException(string.Format(CommonMessages.Contributions.SplitPaymentSumMismatchFormat, request.CashAmount.Value, request.UpiAmount.Value, splitSum, targetAmount));
                }
            }

            var paymentDate = request.PaymentDate?.ToUniversalTime() ?? DateTime.UtcNow;

            // Handle Multi-Event Settlement (PreviousArrears / AllOutstanding)
            if (!string.IsNullOrWhiteSpace(request.PaymentScope) &&
                (request.PaymentScope.Equals(CommonConstants.PaymentScopes.PreviousArrears, StringComparison.OrdinalIgnoreCase) ||
                 request.PaymentScope.Equals(CommonConstants.PaymentScopes.AllOutstanding, StringComparison.OrdinalIgnoreCase)))
            {
                var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
                var memberDtos = allContributions
                    .Where(c => c.MemberId == request.MemberId)
                    .ToList();

                List<ContributionDto> pendingDtos;

                if (request.PaymentScope.Equals(CommonConstants.PaymentScopes.PreviousArrears, StringComparison.OrdinalIgnoreCase))
                {
                    pendingDtos = memberDtos
                        .Where(c => c.EventId != request.EventId && c.PaymentStatus != PaymentStatus.Paid)
                        .ToList();
                }
                else
                {
                    pendingDtos = memberDtos
                        .Where(c => c.PaymentStatus != PaymentStatus.Paid)
                        .OrderBy(c => c.EventId == request.EventId ? 1 : 0)
                        .ToList();
                }

                List<Contribution> pendingToPay = new();
                foreach (var dto in pendingDtos)
                {
                    var entity = await _contributionRepository.GetByEventAndMemberAsync(dto.EventId, dto.MemberId, cancellationToken);
                    if (entity != null)
                    {
                        pendingToPay.Add(entity);
                    }
                }

                decimal remainingAllocation = targetAmount;

                foreach (var item in pendingToPay)
                {
                    if (remainingAllocation <= 0) break;

                    decimal dueForThis = item.Amount;
                    decimal paidForThis = Math.Min(remainingAllocation, dueForThis);
                    remainingAllocation -= paidForThis;

                    item.PaymentStatus = PaymentStatus.Paid;
                    item.PaymentDate = paymentDate;
                    item.PaymentMode = request.PaymentMode;

                    if (request.PaymentMode == PaymentMode.Split && targetAmount > 0)
                    {
                        decimal ratio = paidForThis / targetAmount;
                        item.CashAmount = Math.Round((request.CashAmount ?? 0) * ratio, 2);
                        item.UpiAmount = Math.Round((request.UpiAmount ?? 0) * ratio, 2);
                    }
                    else
                    {
                        item.CashAmount = null;
                        item.UpiAmount = null;
                    }

                    _contributionRepository.Update(item);
                }

                // If current contribution wasn't in pendingToPay (already paid), update its details
                if (!pendingToPay.Any(x => x.ContributionId == contribution.ContributionId))
                {
                    contribution.PaymentDate = paymentDate;
                    contribution.PaymentMode = request.PaymentMode;
                    if (request.PaymentMode == PaymentMode.Split)
                    {
                        contribution.CashAmount = request.CashAmount;
                        contribution.UpiAmount = request.UpiAmount;
                    }
                    _contributionRepository.Update(contribution);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return _mapper.Map<ContributionDto>(contribution);
            }

            // Standard Single Event Payment
            if (request.PaymentMode == PaymentMode.Split)
            {
                contribution.CashAmount = request.CashAmount ?? 0;
                contribution.UpiAmount = request.UpiAmount ?? 0;
            }
            else
            {
                contribution.CashAmount = null;
                contribution.UpiAmount = null;
            }

            if (request.Amount.HasValue)
            {
                contribution.Amount = request.Amount.Value;
            }

            contribution.PaymentStatus = PaymentStatus.Paid;
            contribution.PaymentDate = paymentDate;
            contribution.PaymentMode = request.PaymentMode;

            _contributionRepository.Update(contribution);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ContributionDto>(contribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(PayAsync));
            throw;
        }
    }

    public async Task<MemberContributionSummaryDto> GetMySummaryAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try
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

            var firstContribution = contributions.First();
            var paidContributions = contributions.Where(c => c.PaymentStatus == PaymentStatus.Paid).ToList();

            var categoryBreakdown = paidContributions
                .GroupBy(c => !string.IsNullOrWhiteSpace(c.CategoryName) ? c.CategoryName : CommonConstants.Defaults.Uncategorized)
                .Select(g =>
                {
                    var first = g.FirstOrDefault();
                    return new ContributionCategoryBreakdownDto
                    {
                        CategoryName = g.Key,
                        TotalPaid = g.Sum(c => c.Amount),
                        EventCount = g.Select(c => c.EventId).Distinct().Count(),
                        CreatedBy = first?.CreatedBy ?? firstContribution.CreatedBy,
                        CreatedAt = first?.CreatedAt ?? firstContribution.CreatedAt
                    };
                })
                .OrderByDescending(x => x.TotalPaid)
                .ToList();

            var eventBreakdown = contributions
                .Select(c => new ContributionEventBreakdownDto
                {
                    EventName = !string.IsNullOrWhiteSpace(c.EventName) ? c.EventName : CommonConstants.Defaults.UnknownEvent,
                    CategoryName = !string.IsNullOrWhiteSpace(c.CategoryName) ? c.CategoryName : CommonConstants.Defaults.Uncategorized,
                    Amount = c.Amount,
                    PaymentStatus = c.PaymentStatus.ToString(),
                    PaymentDate = c.PaymentDate,
                    CreatedBy = c.CreatedBy,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            return new MemberContributionSummaryDto
            {
                MemberId = firstContribution.MemberId,
                MemberName = firstContribution.MemberName,
                TotalPaidAmount = paidContributions.Sum(c => c.Amount),
                TotalPendingAmount = contributions.Where(c => c.PaymentStatus != PaymentStatus.Paid).Sum(c => c.Amount),
                CategoryBreakdown = categoryBreakdown,
                EventBreakdown = eventBreakdown
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMySummaryAsync));
            throw;
        }
    }
}
