using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class ContributionService : IContributionService
{
    private readonly Microsoft.Extensions.Logging.ILogger<ContributionService> _logger;
    private readonly IContributionRepository _contributionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ContributionService(Microsoft.Extensions.Logging.ILogger<ContributionService> logger, IContributionRepository contributionRepository, IUnitOfWork unitOfWork, IMapper mapper)
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
            var contributions = await _contributionRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<ContributionDto>>(contributions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contributions = await _contributionRepository.GetByEventIdAsync(eventId, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<ContributionDto>>(contributions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByEventIdAsync");
            throw;
        }
    }

    public async Task<ContributionDto> PayAsync(PayContributionRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var contribution = await _contributionRepository.GetByEventAndMemberAsync(request.EventId, request.MemberId, cancellationToken)
            ?? throw new KeyNotFoundException("Contribution record not found.");

            var targetAmount = request.Amount ?? contribution.Amount;

            if (request.PaymentMode == PaymentMode.Split)
            {
                if (!request.CashAmount.HasValue || !request.UpiAmount.HasValue)
                {
                    throw new ArgumentException("Both Cash Amount and UPI Amount must be provided for Split Payment.");
                }

                if (request.CashAmount.Value < 0 || request.UpiAmount.Value < 0)
                {
                    throw new ArgumentException("Split payment amounts cannot be negative.");
                }

                var splitSum = Math.Round(request.CashAmount.Value + request.UpiAmount.Value, 2);
                if (splitSum != Math.Round(targetAmount, 2))
                {
                    throw new ArgumentException($"Cash (₹{request.CashAmount.Value}) + UPI (₹{request.UpiAmount.Value}) = ₹{splitSum} must equal Total Amount (₹{targetAmount}).");
                }
            }

            var paymentDate = request.PaymentDate?.ToUniversalTime() ?? DateTime.UtcNow;

            // Handle Multi-Event Settlement (PreviousArrears / AllOutstanding)
            if (!string.IsNullOrWhiteSpace(request.PaymentScope) &&
                (request.PaymentScope.Equals("PreviousArrears", StringComparison.OrdinalIgnoreCase) ||
                 request.PaymentScope.Equals("AllOutstanding", StringComparison.OrdinalIgnoreCase)))
            {
                var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
                var memberContributions = allContributions
                    .Where(c => c.MemberId == request.MemberId && !c.IsDeleted)
                    .ToList();

                List<Contribution> pendingToPay;

                if (request.PaymentScope.Equals("PreviousArrears", StringComparison.OrdinalIgnoreCase))
                {
                    pendingToPay = memberContributions
                        .Where(c => c.EventId != request.EventId && c.PaymentStatus != PaymentStatus.Paid)
                        .OrderBy(c => c.Event != null ? c.Event.EventDate : DateTime.MinValue)
                        .ToList();
                }
                else
                {
                    // AllOutstanding: previous arrears first, then current event
                    pendingToPay = memberContributions
                        .Where(c => c.PaymentStatus != PaymentStatus.Paid)
                        .OrderBy(c => c.EventId == request.EventId ? 1 : 0)
                        .ThenBy(c => c.Event != null ? c.Event.EventDate : DateTime.MinValue)
                        .ToList();
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
            _logger.LogError(ex, "Error in PayAsync");
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

        var member = contributions.First().Member!;
        var paidContributions = contributions.Where(c => c.PaymentStatus == PaymentStatus.Paid).ToList();

        var categoryBreakdown = paidContributions
            .GroupBy(c => c.Event?.EventType?.EventTypeName ?? "Uncategorized")
            .Select(g =>
            {
                var first = g.FirstOrDefault();
                return new ContributionCategoryBreakdownDto
                {
                    CategoryName = g.Key,
                    TotalPaid = g.Sum(c => c.Amount),
                    EventCount = g.Select(c => c.EventId).Distinct().Count(),
                    CreatedBy = first?.CreatedBy ?? member.CreatedBy,
                    CreatedAt = first?.CreatedAt ?? member.CreatedAt
                };
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
                PaymentDate = c.PaymentDate,
                CreatedBy = c.CreatedBy ?? (c.Event?.CreatedByUser != null ? c.Event.CreatedByUser.FullName : null),
                CreatedAt = c.CreatedAt
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMySummaryAsync");
            throw;
        }
    }
}
