using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.Interfaces.Common;
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
    private readonly ICurrentUserService? _currentUserService;
    private readonly IMemberRepository? _memberRepository;
    private readonly IStatusRepository? _statusRepository;
    private readonly IPaymentModeRepository? _paymentModeRepository;
    private readonly IEventRepository? _eventRepository;

    public ContributionService(
        ILogger<ContributionService> logger,
        IContributionRepository contributionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService? currentUserService = null,
        IMemberRepository? memberRepository = null,
        IStatusRepository? statusRepository = null,
        IPaymentModeRepository? paymentModeRepository = null,
        IEventRepository? eventRepository = null)
    {
        _logger = logger;
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _memberRepository = memberRepository;
        _statusRepository = statusRepository;
        _paymentModeRepository = paymentModeRepository;
        _eventRepository = eventRepository;
    }

    public async Task<IReadOnlyCollection<ContributionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _contributionRepository.GetAllAsync(cancellationToken);
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                if (myMemberId.HasValue)
                {
                    return all.Where(c => c.MemberId == myMemberId.Value).ToList();
                }

                var userEmail = _currentUserService.Email;
                if (!string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    var myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                    if (myMember != null)
                    {
                        return all.Where(c => c.MemberId == myMember.MemberId).ToList();
                    }
                }
                return Array.Empty<ContributionDto>();
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    return all.Where(c => myEventIds.Contains(c.EventId)).ToList();
                }
                return Array.Empty<ContributionDto>();
            }

            return all;
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
            var byEvent = await _contributionRepository.GetByEventIdAsync(eventId, cancellationToken);
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                if (myMemberId.HasValue)
                {
                    return byEvent.Where(c => c.MemberId == myMemberId.Value).ToList();
                }

                var userEmail = _currentUserService.Email;
                if (!string.IsNullOrWhiteSpace(userEmail) && _memberRepository != null)
                {
                    var myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                    if (myMember != null)
                    {
                        return byEvent.Where(c => c.MemberId == myMember.MemberId).ToList();
                    }
                }
                return Array.Empty<ContributionDto>();
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var isMyEvent = myEvents.Any(e => e.EventId == eventId);
                    if (!isMyEvent)
                    {
                        return Array.Empty<ContributionDto>();
                    }
                }
            }

            return byEvent;
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
            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    if (!myEvents.Any(e => e.EventId == request.EventId))
                    {
                        throw new UnauthorizedAccessException("You are only permitted to record payments for events you organize.");
                    }
                }
            }

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

                var splitSum = RoundingHelper.RoundToWhole(request.CashAmount.Value + request.UpiAmount.Value);
                if (splitSum != RoundingHelper.RoundToWhole(targetAmount))
                {
                    throw new ArgumentException(string.Format(CommonMessages.Contributions.SplitPaymentSumMismatchFormat, request.CashAmount.Value, request.UpiAmount.Value, splitSum, targetAmount));
                }
            }

            var paymentDate = request.PaymentDate?.ToUniversalTime() ?? DateTime.UtcNow;

            Guid? paidStatusId = null;
            if (_statusRepository != null)
            {
                var st = await _statusRepository.GetByNameAsync("Paid", cancellationToken);
                paidStatusId = st?.StatusId;
            }

            Guid? paymentModeId = null;
            if (_paymentModeRepository != null)
            {
                var modeName = request.PaymentMode switch
                {
                    PaymentMode.Cash => "Cash",
                    PaymentMode.Split => "Split",
                    _ => "UPI"
                };
                var pm = await _paymentModeRepository.GetByNameAsync(modeName, cancellationToken);
                paymentModeId = pm?.PaymentModeId;
            }

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

                    if (paidForThis >= dueForThis)
                    {
                        item.StatusId = paidStatusId;
                        item.PaymentStatus = PaymentStatus.Paid;
                    }
                    else
                    {
                        item.PaymentStatus = PaymentStatus.Pending;
                    }

                    item.PaymentModeId = paymentModeId;
                    item.PaymentDate = paymentDate;
                    item.PaymentMode = request.PaymentMode;

                    if (request.PaymentMode == PaymentMode.Split && targetAmount > 0)
                    {
                        decimal ratio = paidForThis / targetAmount;
                        item.CashAmount = RoundingHelper.RoundToWhole((request.CashAmount ?? 0) * ratio);
                        item.UpiAmount = RoundingHelper.RoundToWhole((request.UpiAmount ?? 0) * ratio);
                    }
                    else if (request.PaymentMode == PaymentMode.Cash)
                    {
                        item.CashAmount = paidForThis;
                        item.UpiAmount = 0m;
                    }
                    else
                    {
                        item.UpiAmount = paidForThis;
                        item.CashAmount = 0m;
                    }

                    _contributionRepository.Update(item);
                }

                // If current contribution wasn't in pendingToPay (already paid), update its details
                if (!pendingToPay.Any(x => x.ContributionId == contribution.ContributionId))
                {
                    contribution.StatusId = paidStatusId;
                    contribution.PaymentModeId = paymentModeId;
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

            contribution.StatusId = paidStatusId;
            contribution.PaymentModeId = paymentModeId;
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

    private async Task<Guid?> GetOrganizerIdAsync(CancellationToken cancellationToken)
    {
        if (_currentUserService == null) return null;
        if (!string.IsNullOrWhiteSpace(_currentUserService.UserId) && Guid.TryParse(_currentUserService.UserId, out var parsedGuid))
        {
            return parsedGuid;
        }
        if (_currentUserService.MemberId.HasValue)
        {
            return _currentUserService.MemberId.Value;
        }
        if (!string.IsNullOrWhiteSpace(_currentUserService.Email) && _memberRepository != null)
        {
            var myMember = await _memberRepository.GetByEmailAsync(_currentUserService.Email.Trim(), cancellationToken);
            if (myMember != null)
            {
                return myMember.MemberId;
            }
        }
        return null;
    }
}
