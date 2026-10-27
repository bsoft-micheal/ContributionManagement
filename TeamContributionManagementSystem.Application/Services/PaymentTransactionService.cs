using System.IO;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.Exceptions;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class PaymentTransactionService : IPaymentTransactionService
{
    private readonly ILogger<PaymentTransactionService> _logger;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ISystemSettingService _settingService;
    private readonly IStatusRepository? _statusRepository;
    private readonly IPaymentModeRepository? _paymentModeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService? _currentUserService;

    public PaymentTransactionService(
        ILogger<PaymentTransactionService> logger,
        IPaymentTransactionRepository transactionRepository,
        IContributionRepository contributionRepository,
        IEventRepository eventRepository,
        IMemberRepository memberRepository,
        ISystemSettingService settingService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IStatusRepository? statusRepository = null,
        IPaymentModeRepository? paymentModeRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _contributionRepository = contributionRepository;
        _eventRepository = eventRepository;
        _memberRepository = memberRepository;
        _settingService = settingService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _statusRepository = statusRepository;
        _paymentModeRepository = paymentModeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _transactionRepository.GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                if (myMember != null)
                {
                    var memberName = myMember.Name.Trim();
                    return all.Where(t => string.Equals(t.MemberName, memberName, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                return Array.Empty<PaymentTransactionDto>();
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet();

                    return all.Where(t =>
                        (t.EventId.HasValue && myEventIds.Contains(t.EventId.Value)) ||
                        (!string.IsNullOrWhiteSpace(t.EventName) && myEventNames.Contains(t.EventName.Trim().ToLower()))
                    ).ToList();
                }
                return Array.Empty<PaymentTransactionDto>();
            }
            return all;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<PaymentTransactionDto> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Payments.NotFound);

            if (_currentUserService != null && _currentUserService.IsMemberRole)
            {
                var myMemberId = _currentUserService.MemberId;
                Domain.Entities.Member? myMember = null;
                if (myMemberId.HasValue)
                {
                    myMember = await _memberRepository.GetByIdAsync(myMemberId.Value, cancellationToken);
                }

                var userEmail = _currentUserService.Email;
                if (myMember == null && !string.IsNullOrWhiteSpace(userEmail))
                {
                    myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                }

                bool isOwner = myMember != null && string.Equals(entity.MemberName, myMember.Name.Trim(), StringComparison.OrdinalIgnoreCase);

                if (!isOwner)
                {
                    throw new UnauthorizedAccessException("Access denied to requested payment transaction.");
                }
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet();

                    var isMyEvent = (entity.EventId.HasValue && myEventIds.Contains(entity.EventId.Value)) ||
                                    (!string.IsNullOrWhiteSpace(entity.EventName) && myEventNames.Contains(entity.EventName.Trim().ToLower()));
                    if (!isMyEvent)
                    {
                        throw new UnauthorizedAccessException("Access denied to requested payment transaction.");
                    }
                }
            }

            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
            throw;
        }
    }

    public async Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Amount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero.");
            }

            if (request.Amount % 1 != 0)
            {
                throw new ArgumentException("Payment amount cannot contain decimal or point values. Only whole numbers (₹) are allowed.");
            }

            if (!string.IsNullOrWhiteSpace(request.Utr))
            {
                var existingTxns = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
                var dup = existingTxns.FirstOrDefault(t =>
                    !string.IsNullOrWhiteSpace(t.Utr) &&
                    string.Equals(t.Utr, request.Utr.Trim(), StringComparison.OrdinalIgnoreCase));
                if (dup != null)
                {
                    throw new AlreadyProcessedException("This action has already been completed.");
                }
            }

            var txnNumber = await _transactionRepository.GetNextTxnNumberAsync(request.PaymentDate, cancellationToken);

            var status = request.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status) && _statusRepository != null)
            {
                var dbStatuses = await _statusRepository.GetAllAsync(true, cancellationToken);
                status = dbStatuses.FirstOrDefault(s => s.StatusName.Equals("Pending", StringComparison.OrdinalIgnoreCase))?.StatusName
                    ?? dbStatuses.FirstOrDefault()?.StatusName
                    ?? "Pending";
            }
            else if (string.IsNullOrWhiteSpace(status))
            {
                status = "Pending";
            }

            // Resolve UserId from name or user string
            Guid? resolvedUserId = null;
            if (!string.IsNullOrWhiteSpace(request.MemberName))
            {
                var matchedUser = await _memberRepository.GetUserByNameAsync(request.MemberName.Trim(), cancellationToken);
                resolvedUserId = matchedUser?.UserId;
            }

            // Resolve EventId from name
            Guid? resolvedEventId = null;
            if (!string.IsNullOrWhiteSpace(request.EventName))
            {
                var matchedEvent = await _eventRepository.GetByNameAsync(request.EventName.Trim(), cancellationToken);
                resolvedEventId = matchedEvent?.EventId;
            }

            // Resolve PaymentModeId
            Guid? resolvedPaymentModeId = null;
            var paymentModeName = request.PaymentMode?.Trim() ?? "UPI";
            if (_paymentModeRepository != null && !string.IsNullOrWhiteSpace(paymentModeName))
            {
                var matchedPm = await _paymentModeRepository.GetByNameAsync(paymentModeName, cancellationToken);
                resolvedPaymentModeId = matchedPm?.PaymentModeId;
            }

            // Resolve StatusId
            Guid? resolvedStatusId = null;
            if (_statusRepository != null && !string.IsNullOrWhiteSpace(status))
            {
                var matchedSt = await _statusRepository.GetByNameAsync(status, cancellationToken);
                resolvedStatusId = matchedSt?.StatusId;
            }

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = request.MemberName?.Trim() ?? string.Empty,
                EventName = request.EventName?.Trim() ?? string.Empty,
                UserId = resolvedUserId,
                EventId = resolvedEventId,
                PaymentModeId = resolvedPaymentModeId,
                StatusId = resolvedStatusId,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate,
                PaymentMode = paymentModeName,
                Utr = request.Utr?.Trim(),
                Status = status,
                Notes = request.Notes?.Trim(),
                Screenshot = await SaveScreenshotAsync(request.Screenshot, txnNumber, cancellationToken),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow
            };

            if (entity.Status == CommonConstants.PaymentStatuses.Verified)
            {
                entity.VerifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
                entity.VerifiedOn = DateTime.UtcNow;
            }

            await _transactionRepository.AddAsync(entity, cancellationToken);
            await SyncContributionFromTransactionAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<PaymentTransactionDto> SubmitProofAsync(SubmitPaymentProofDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Amount <= 0)
            {
                throw new ArgumentException("Contribution amount must be greater than zero.");
            }

            if (request.Amount % 1 != 0)
            {
                throw new ArgumentException("Contribution amount cannot contain decimal or point values. Only whole numbers (₹) are allowed.");
            }

            if (request.CashAmount.HasValue && request.CashAmount.Value % 1 != 0)
            {
                throw new ArgumentException("Cash amount cannot contain decimal or point values. Only whole numbers (₹) are allowed.");
            }

            if (request.UpiAmount.HasValue && request.UpiAmount.Value % 1 != 0)
            {
                throw new ArgumentException("UPI amount cannot contain decimal or point values. Only whole numbers (₹) are allowed.");
            }

            if (!string.IsNullOrWhiteSpace(request.Utr))
            {
                var existingTxns = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
                var dup = existingTxns.FirstOrDefault(t =>
                    !string.IsNullOrWhiteSpace(t.Utr) &&
                    string.Equals(t.Utr, request.Utr.Trim(), StringComparison.OrdinalIgnoreCase));
                if (dup != null)
                {
                    throw new AlreadyProcessedException("This action has already been completed.");
                }
            }

            var existingAll = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
            var rapidDup = existingAll.FirstOrDefault(t =>
                string.Equals(t.MemberName, request.MemberName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.EventName, request.EventName?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                t.Amount == request.Amount &&
                t.CreatedAt.HasValue &&
                Math.Abs((DateTime.UtcNow - t.CreatedAt.Value).TotalSeconds) < 5);
            if (rapidDup != null)
            {
                throw new AlreadyProcessedException("This action has already been completed.");
            }

            var hasSplits = request.Splits != null && request.Splits.Count > 1;

            if (hasSplits)
            {
                var splitTotal = request.Splits!.Sum(s => s.Amount);
                if (Math.Abs(splitTotal - request.Amount) > 0.01m)
                {
                    throw new ArgumentException($"Split amounts total (₹{splitTotal:N2}) does not match the submitted total (₹{request.Amount:N2}).");
                }

                foreach (var split in request.Splits!)
                {
                    if (split.Amount <= 0)
                    {
                        throw new ArgumentException("Each split payment amount must be greater than zero.");
                    }

                    if (split.Amount % 1 != 0)
                    {
                        throw new ArgumentException("Split payment amount cannot contain decimal or point values. Only whole numbers (₹) are allowed.");
                    }

                    if (string.IsNullOrWhiteSpace(split.Mode))
                    {
                        throw new ArgumentException("Payment mode is required for all split rows.");
                    }

                    var isCash = split.Mode.Trim().Contains("Cash", StringComparison.OrdinalIgnoreCase);
                    if (!isCash)
                    {
                        if (string.IsNullOrWhiteSpace(split.Utr) || split.Utr.Trim().Length < 6)
                        {
                            throw new ArgumentException($"A valid UTR / reference number (at least 6 characters) is required for digital payment mode '{split.Mode}'.");
                        }
                    }
                }

                var digitalUtrs = request.Splits
                    .Where(s => !s.Mode.Trim().Contains("Cash", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(s.Utr))
                    .Select(s => s.Utr.Trim().ToLowerInvariant())
                    .ToList();

                if (digitalUtrs.Count != digitalUtrs.Distinct().Count())
                {
                    throw new ArgumentException("Same UTR number cannot be used for different payment modes.");
                }
            }

            // Resolve EventId — verify event exists in DB before setting FK to prevent constraint violation
            Guid? resolvedEventId = null;
            string resolvedEventName = request.EventName?.Trim() ?? string.Empty;
            if (request.EventId.HasValue && request.EventId.Value != Guid.Empty)
            {
                var ev = await _eventRepository.GetByIdAsync(request.EventId.Value, cancellationToken);
                if (ev != null)
                {
                    resolvedEventId = ev.EventId;
                    if (string.IsNullOrWhiteSpace(resolvedEventName)) resolvedEventName = ev.EventName;
                }
            }
            if (!resolvedEventId.HasValue && !string.IsNullOrWhiteSpace(resolvedEventName))
            {
                var ev = await _eventRepository.GetByNameAsync(resolvedEventName, cancellationToken);
                if (ev != null)
                {
                    resolvedEventId = ev.EventId;
                }
            }

            // Resolve UserId — verify user exists in DB before setting FK to prevent constraint violation
            Guid? resolvedUserId = null;
            string resolvedMemberName = request.MemberName?.Trim() ?? string.Empty;
            if (request.MemberId.HasValue && request.MemberId.Value != Guid.Empty)
            {
                var user = await _memberRepository.GetByIdAsync(request.MemberId.Value, cancellationToken);
                if (user != null)
                {
                    resolvedUserId = user.MemberId;
                    if (string.IsNullOrWhiteSpace(resolvedMemberName)) resolvedMemberName = user.Name;
                }
            }
            if (!resolvedUserId.HasValue && !string.IsNullOrWhiteSpace(resolvedMemberName))
            {
                var mem = await _memberRepository.GetUserByNameAsync(resolvedMemberName, cancellationToken);
                if (mem != null)
                {
                    resolvedUserId = mem.UserId;
                }
            }

            // Resolve StatusId for Pending
            Guid? resolvedStatusId = null;
            Status? pendingStatus = null;
            if (_statusRepository != null)
            {
                pendingStatus = await _statusRepository.GetByNameAsync(CommonConstants.PaymentStatuses.Pending, cancellationToken);
                resolvedStatusId = pendingStatus?.StatusId;
            }

            var safeNotes = request.Notes?.Trim();
            if (!string.IsNullOrEmpty(request.PaymentScope))
            {
                var scopeTag = $"[Scope: {request.PaymentScope.Trim()}]";
                safeNotes = string.IsNullOrEmpty(safeNotes) ? scopeTag : $"{scopeTag} {safeNotes}";
            }
            if (!string.IsNullOrEmpty(safeNotes) && safeNotes.Length > 990)
            {
                safeNotes = safeNotes.Substring(0, 985) + "...";
            }

            // Concurrency-safe base transaction number generation (Requirement 5)
            var baseTxn = await _transactionRepository.GetNextTxnNumberAsync(request.PaymentDate, cancellationToken);
            var savedScreenshot = await SaveScreenshotAsync(request.Screenshot, baseTxn, cancellationToken);

            var createdEntities = new List<PaymentTransaction>();

            if (hasSplits)
            {
                // Requirement 4: One logical payment group, separate child transaction rows. No additional master row.
                var groupId = Guid.NewGuid();
                int index = 1;

                foreach (var split in request.Splits!)
                {
                    var splitModeName = split.Mode.Trim();
                    Guid? splitModeId = null;
                    if (_paymentModeRepository != null)
                    {
                        var matchedPm = await _paymentModeRepository.GetByNameAsync(splitModeName, cancellationToken);
                        splitModeId = matchedPm?.PaymentModeId;
                    }

                    var isCash = splitModeName.Contains("Cash", StringComparison.OrdinalIgnoreCase);
                    var childUtr = isCash ? null : split.Utr?.Trim();
                    if (!string.IsNullOrEmpty(childUtr) && childUtr.Length > 95)
                    {
                        childUtr = childUtr.Substring(0, 92) + "...";
                    }

                    var childNotes = isCash && !string.IsNullOrWhiteSpace(split.Notes)
                        ? (string.IsNullOrWhiteSpace(safeNotes) ? $"[Cash Note: {split.Notes.Trim()}]" : $"{safeNotes} [Cash Note: {split.Notes.Trim()}]")
                        : safeNotes;
                    if (!string.IsNullOrEmpty(childNotes) && childNotes.Length > 990)
                    {
                        childNotes = childNotes.Substring(0, 985) + "...";
                    }

                    // Concurrency-safe distinct unique transaction ID generation in sequential order without hyphens (e.g. TXN2026020001, TXN2026020002)
                    var childTxnNumber = index == 1
                        ? baseTxn
                        : await _transactionRepository.GetNextTxnNumberAsync(request.PaymentDate, cancellationToken);

                    var childEntity = new PaymentTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        TxnNumber = childTxnNumber,
                        ParentTxnNumber = baseTxn,
                        TransactionGroupId = groupId,
                        MemberName = resolvedMemberName.Length > 150 ? resolvedMemberName.Substring(0, 150) : resolvedMemberName,
                        EventName = resolvedEventName.Length > 200 ? resolvedEventName.Substring(0, 200) : resolvedEventName,
                        UserId = resolvedUserId,
                        EventId = resolvedEventId,
                        PaymentModeId = splitModeId,
                        StatusId = resolvedStatusId,
                        StatusItem = pendingStatus,
                        Amount = split.Amount,
                        PaymentDate = request.PaymentDate != default ? request.PaymentDate : DateTime.UtcNow,
                        PaymentMode = splitModeName.Length > 50 ? splitModeName.Substring(0, 50) : splitModeName,
                        Utr = childUtr,
                        Status = CommonConstants.PaymentStatuses.Pending,
                        Notes = childNotes,
                        Screenshot = savedScreenshot,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedBy = resolvedUserId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _transactionRepository.AddAsync(childEntity, cancellationToken);
                    createdEntities.Add(childEntity);
                    index++;
                }

                // Synchronize contribution for the group atomically (Requirement 8 & 9)
                await SyncContributionForTransactionGroupAsync(createdEntities, cancellationToken);
            }
            else
            {
                // Single-mode payment (Requirement 12: preserve single-mode payments and backward compatibility)
                var singleMode = request.Splits != null && request.Splits.Count == 1 ? request.Splits[0].Mode : request.PaymentMode;
                var singleUtr = request.Splits != null && request.Splits.Count == 1 ? request.Splits[0].Utr : request.Utr;
                var paymentModeName = string.IsNullOrWhiteSpace(singleMode) ? CommonConstants.PaymentModes.Upi : singleMode.Trim();
                
                Guid? resolvedPaymentModeId = null;
                if (_paymentModeRepository != null && !string.IsNullOrWhiteSpace(paymentModeName))
                {
                    var matchedPm = await _paymentModeRepository.GetByNameAsync(paymentModeName, cancellationToken);
                    resolvedPaymentModeId = matchedPm?.PaymentModeId;
                }

                var isCash = paymentModeName.Contains("Cash", StringComparison.OrdinalIgnoreCase);
                var safeUtr = isCash ? null : singleUtr?.Trim();
                if (!string.IsNullOrEmpty(safeUtr) && safeUtr.Length > 95)
                {
                    safeUtr = safeUtr.Substring(0, 92) + "...";
                }

                var entity = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    TxnNumber = baseTxn,
                    ParentTxnNumber = null,
                    TransactionGroupId = null,
                    MemberName = resolvedMemberName.Length > 150 ? resolvedMemberName.Substring(0, 150) : resolvedMemberName,
                    EventName = resolvedEventName.Length > 200 ? resolvedEventName.Substring(0, 200) : resolvedEventName,
                    UserId = resolvedUserId,
                    EventId = resolvedEventId,
                    PaymentModeId = resolvedPaymentModeId,
                    StatusId = resolvedStatusId,
                    StatusItem = pendingStatus,
                    Amount = request.Amount,
                    PaymentDate = request.PaymentDate != default ? request.PaymentDate : DateTime.UtcNow,
                    PaymentMode = paymentModeName.Length > 50 ? paymentModeName.Substring(0, 50) : paymentModeName,
                    Utr = safeUtr,
                    Status = CommonConstants.PaymentStatuses.Pending,
                    Notes = safeNotes,
                    Screenshot = savedScreenshot,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedBy = resolvedUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepository.AddAsync(entity, cancellationToken);
                createdEntities.Add(entity);

                await SyncContributionForTransactionGroupAsync(createdEntities, cancellationToken);
            }

            // Requirement 6: Save child rows atomically using Unit of Work
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Payments.PaymentProofSubmitted,
                baseTxn, resolvedMemberName, resolvedEventName, request.Amount, request.Utr);

            var primaryDto = _mapper.Map<PaymentTransactionDto>(createdEntities.First());
            return primaryDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(SubmitProofAsync));
            throw;
        }
    }

    public async Task<PaymentContextDto?> GetPaymentContextAsync(Guid? eventId, Guid? memberId, string? eventName = null, string? memberName = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new PaymentContextDto();

            result.QrImage = string.Empty;

            Event? ev = null;
            if (eventId.HasValue && eventId.Value != Guid.Empty)
            {
                ev = await _eventRepository.GetByIdAsync(eventId.Value, cancellationToken);
            }
            if (ev == null && !string.IsNullOrWhiteSpace(eventName))
            {
                ev = await _eventRepository.GetByNameAsync(eventName.Trim(), cancellationToken);
            }
            if (ev != null)
            {
                result.EventId = ev.EventId;
                result.EventName = ev.EventName;
                result.Amount = ev.BaseAmount;
                result.CurrentEventDue = ev.BaseAmount;
                result.TotalDue = ev.BaseAmount;
            }

            Member? mem = null;
            if (memberId.HasValue && memberId.Value != Guid.Empty)
            {
                mem = await _memberRepository.GetByIdAsync(memberId.Value, cancellationToken);
            }
            if (mem == null && !string.IsNullOrWhiteSpace(memberName))
            {
                var user = await _memberRepository.GetUserByNameAsync(memberName.Trim(), cancellationToken);
                if (user != null)
                {
                    mem = await _memberRepository.GetByIdAsync(user.UserId, cancellationToken);
                }
            }
            if (mem != null)
            {
                result.MemberId = mem.MemberId;
                result.MemberName = mem.Name;
                result.Email = mem.Email;
            }

            var effectiveEventId = result.EventId;
            var effectiveMemberId = result.MemberId;

            // Check specific contribution record
            if (effectiveEventId.HasValue && effectiveMemberId.HasValue)
            {
                var contrib = await _contributionRepository.GetByEventAndMemberAsync(effectiveEventId.Value, effectiveMemberId.Value, cancellationToken);
                if (contrib != null)
                {
                    result.Amount = contrib.Amount;
                    result.Status = contrib.StatusItem?.StatusName ?? contrib.PaymentStatus.ToString();
                    var paidSum = (contrib.CashAmount ?? 0) + (contrib.UpiAmount ?? 0);
                    var isPaid = result.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                                 result.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase) ||
                                 result.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                                 result.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                 contrib.PaymentStatus == PaymentStatus.Paid ||
                                 (contrib.Amount > 0 && paidSum >= contrib.Amount);
                    result.CurrentEventDue = isPaid ? 0 : Math.Max(0, contrib.Amount - paidSum);
                    if (isPaid && !result.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Status = "Paid";
                    }
                    else if (!isPaid && paidSum > 0)
                    {
                        result.Status = "Partial";
                    }
                }
            }

            // Calculate previous arrears
            if (!string.IsNullOrWhiteSpace(result.Email))
            {
                try
                {
                    var pastContribs = await _contributionRepository.GetByMemberEmailAsync(result.Email, cancellationToken);
                    var allTxns = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);

                    List<ContributionDto> arrearsList = pastContribs
                        .Where(c => c.EventId != effectiveEventId)
                        .Where(c =>
                        {
                            var paidSum = (c.CashAmount ?? 0) + (c.UpiAmount ?? 0);
                            if (c.PaymentStatus == PaymentStatus.Paid ||
                                string.Equals(c.StatusName, "Paid", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(c.StatusName, "Verified", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(c.StatusName, "Closed", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(c.StatusName, "Completed", StringComparison.OrdinalIgnoreCase) ||
                                (c.Amount > 0 && paidSum >= c.Amount))
                            {
                                return false;
                            }

                            var hasTx = allTxns.Any(t =>
                                ((t.EventId.HasValue && t.EventId.Value == c.EventId) ||
                                 (!string.IsNullOrWhiteSpace(t.EventName) && !string.IsNullOrWhiteSpace(c.EventName) && t.EventName.Trim().Equals(c.EventName.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                                ((t.UserId.HasValue && t.UserId.Value == c.MemberId) ||
                                 (!string.IsNullOrWhiteSpace(t.MemberName) && !string.IsNullOrWhiteSpace(c.MemberName) && t.MemberName.Trim().Equals(c.MemberName.Trim(), StringComparison.OrdinalIgnoreCase))) &&
                                !string.Equals(t.Status, "Rejected", StringComparison.OrdinalIgnoreCase) &&
                                t.Amount >= c.Amount);

                            return !hasTx;
                        })
                        .ToList();

                    result.PreviousArrears = arrearsList.Sum(c => c.Amount);
                    result.ArrearBreakdown = arrearsList.Select(c => new ArrearItemDto
                    {
                        EventId = c.EventId,
                        EventName = string.IsNullOrWhiteSpace(c.EventName) ? "Event" : c.EventName,
                        Amount = c.Amount,
                        EventDate = c.PaymentDate
                    }).ToList();
                }
                catch { }
            }

            result.TotalDue = result.CurrentEventDue + result.PreviousArrears;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetPaymentContextAsync));
            throw;
        }
    }

    public async Task<PaymentTransactionDto> VerifyAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (entity == null)
            {
                var existingAny = await _transactionRepository.GetByIdWithDeletedAsync(transactionId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet();

                    var isMyEvent = (entity.EventId.HasValue && myEventIds.Contains(entity.EventId.Value)) ||
                                    (!string.IsNullOrWhiteSpace(entity.EventName) && myEventNames.Contains(entity.EventName.Trim().ToLower()));
                    if (!isMyEvent)
                    {
                        throw new UnauthorizedAccessException("You are only permitted to verify payments for events you organize.");
                    }
                }
            }

            var reqStatus = request.Status?.Trim() ?? string.Empty;
            if (string.Equals(reqStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                reqStatus = CommonConstants.PaymentStatuses.Verified;
            }
            var targetStatus = string.IsNullOrWhiteSpace(reqStatus) ? CommonConstants.PaymentStatuses.Verified : reqStatus;

            if (string.Equals(entity.Status, targetStatus, StringComparison.OrdinalIgnoreCase))
            {
                throw new AlreadyProcessedException("This action has already been completed.");
            }

            if (request.ModifiedOn.HasValue && entity.ModifiedOn.HasValue)
            {
                if (Math.Abs((entity.ModifiedOn.Value - request.ModifiedOn.Value).TotalSeconds) > 1)
                {
                    throw new RecordModifiedException();
                }
            }

            Guid? statusIdToAssign = null;
            Status? matchedStatus = null;
            if (_statusRepository != null)
            {
                matchedStatus = await _statusRepository.GetByNameAsync(targetStatus, cancellationToken);
                if (matchedStatus == null && string.Equals(targetStatus, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    matchedStatus = await _statusRepository.GetByNameAsync(CommonConstants.PaymentStatuses.Verified, cancellationToken) ?? await _statusRepository.GetByNameAsync("Paid", cancellationToken);
                }
                else if (matchedStatus == null && string.Equals(targetStatus, CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase))
                {
                    matchedStatus = await _statusRepository.GetByNameAsync("Paid", cancellationToken);
                }
                statusIdToAssign = matchedStatus?.StatusId;
            }

            var verifierName = string.IsNullOrWhiteSpace(request.VerifiedBy) ? (string.IsNullOrWhiteSpace(user) ? null : user.Trim()) : request.VerifiedBy.Trim();
            var verifiedTime = DateTime.UtcNow;
            var modifiedByUser = CommonMethods.ParseNullableGuid(verifierName) ?? CommonMethods.ParseNullableGuid(user);

            // Requirement 7: Verifying or rejecting any child updates the entire group consistently
            List<PaymentTransaction> affectedTransactions;
            if (entity.TransactionGroupId.HasValue && entity.TransactionGroupId.Value != Guid.Empty)
            {
                affectedTransactions = await _transactionRepository.GetByGroupIdAsync(entity.TransactionGroupId.Value, cancellationToken);
                if (affectedTransactions.Count == 0)
                {
                    affectedTransactions = new List<PaymentTransaction> { entity };
                }
            }
            else
            {
                affectedTransactions = new List<PaymentTransaction> { entity };
            }

            foreach (var txn in affectedTransactions)
            {
                txn.Status = targetStatus;
                if (statusIdToAssign.HasValue)
                {
                    txn.StatusId = statusIdToAssign.Value;
                    txn.StatusItem = matchedStatus;
                }
                txn.VerifiedBy = verifierName;
                txn.VerifiedOn = verifiedTime;
                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    txn.Notes = request.Notes.Trim();
                }
                txn.ModifiedBy = modifiedByUser;
                txn.ModifiedOn = verifiedTime;
                _transactionRepository.Update(txn);
            }

            // Requirement 8 & 9: Aggregate synchronization and idempotency
            await SyncContributionForTransactionGroupAsync(affectedTransactions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex) when (ex is RecordDeletedException or RecordModifiedException or AlreadyProcessedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(VerifyAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
            if (entity == null)
            {
                var existingAny = await _transactionRepository.GetByIdWithDeletedAsync(transactionId, cancellationToken);
                if (existingAny != null && existingAny.IsDeleted)
                {
                    throw new RecordDeletedException();
                }
                throw new RecordDeletedException();
            }

            if (_currentUserService != null && _currentUserService.IsOrganizerRole && _eventRepository != null)
            {
                var orgId = await GetOrganizerIdAsync(cancellationToken);
                if (orgId.HasValue)
                {
                    var myEvents = await _eventRepository.GetAllForOrganizerAsync(orgId.Value, null, null, cancellationToken);
                    var myEventIds = myEvents.Select(e => e.EventId).ToHashSet();
                    var myEventNames = myEvents.Select(e => e.EventName.Trim().ToLower()).ToHashSet();

                    var isMyEvent = (entity.EventId.HasValue && myEventIds.Contains(entity.EventId.Value)) ||
                                    (!string.IsNullOrWhiteSpace(entity.EventName) && myEventNames.Contains(entity.EventName.Trim().ToLower()));
                    if (!isMyEvent)
                    {
                        throw new UnauthorizedAccessException("You are only permitted to delete transactions for events you organize.");
                    }
                }
            }

            _transactionRepository.Delete(entity);

            // Revert contribution if it was linked
            try
            {
                Contribution? match = null;
                if (entity.EventId.HasValue && entity.UserId.HasValue)
                {
                    match = await _contributionRepository.GetByEventAndMemberAsync(entity.EventId.Value, entity.UserId.Value, cancellationToken);
                }
                if (match != null)
                {
                    match.PaymentStatus = PaymentStatus.Pending;
                    match.UpiAmount = 0;
                    match.CashAmount = 0;
                    if (_statusRepository != null)
                    {
                        var st = await _statusRepository.GetByNameAsync("Pending", cancellationToken);
                        if (st != null) match.StatusId = st.StatusId;
                    }
                    _contributionRepository.Update(match);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revert contribution on transaction delete: {TxnNumber}", entity.TxnNumber);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
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

    private Task SyncContributionFromTransactionAsync(PaymentTransaction entity, CancellationToken cancellationToken)
    {
        return SyncContributionForTransactionGroupAsync(new List<PaymentTransaction> { entity }, cancellationToken);
    }

    private async Task SyncContributionForTransactionGroupAsync(List<PaymentTransaction> groupEntities, CancellationToken cancellationToken)
    {
        if (groupEntities == null || groupEntities.Count == 0) return;

        try
        {
            var primary = groupEntities.First();

            Contribution? match = null;
            if (primary.EventId.HasValue && primary.UserId.HasValue)
            {
                match = await _contributionRepository.GetByEventAndMemberAsync(primary.EventId.Value, primary.UserId.Value, cancellationToken);
            }

            if (match == null && !string.IsNullOrWhiteSpace(primary.MemberName) && !string.IsNullOrWhiteSpace(primary.EventName))
            {
                var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
                var matchDto = allContributions.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.MemberName) &&
                    c.MemberName.Trim().Equals(primary.MemberName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(c.EventName) &&
                    c.EventName.Trim().Equals(primary.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchDto != null)
                {
                    match = await _contributionRepository.GetByEventAndMemberAsync(matchDto.EventId, matchDto.MemberId, cancellationToken);
                }
            }

            // Check if all items in the group are verified/paid/approved
            var isAllVerified = groupEntities.All(t =>
                t.Status.Equals(CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

            var isAnyRejected = groupEntities.Any(t =>
                t.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase));

            var isExplicitlyVerified = !isAnyRejected && groupEntities.Any(t =>
                t.Status.Equals(CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase) ||
                t.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(t.VerifiedBy) && !t.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)));

            // Calculate aggregate group amounts (Requirement 8)
            decimal totalCash = groupEntities
                .Where(t => (t.PaymentMode ?? "").Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            decimal totalUpi = groupEntities
                .Where(t => !(t.PaymentMode ?? "").Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Amount);

            decimal totalPaid = totalCash + totalUpi;
            bool isFullyCovered = !isAnyRejected && match != null && match.Amount > 0 && totalPaid >= match.Amount;

            // Status determination: if member has paid required amount and not rejected, status is Paid (or Verified/Approved if already verified)
            string targetStatusName = isAnyRejected ? "Rejected" : (primary.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : (isExplicitlyVerified ? "Verified" : ((isAllVerified || isFullyCovered) ? "Paid" : "Pending")));
            Guid? statusIdToAssign = null;
            if (_statusRepository != null)
            {
                var st = await _statusRepository.GetByNameAsync(targetStatusName, cancellationToken);
                if (st == null && targetStatusName == "Approved")
                {
                    st = await _statusRepository.GetByNameAsync("Verified", cancellationToken) ?? await _statusRepository.GetByNameAsync("Paid", cancellationToken);
                }
                else if (st == null && targetStatusName == "Verified")
                {
                    st = await _statusRepository.GetByNameAsync("Paid", cancellationToken);
                }
                else if (st == null && targetStatusName == "Paid")
                {
                    st = await _statusRepository.GetByNameAsync("Verified", cancellationToken);
                }
                statusIdToAssign = st?.StatusId;
            }

            PaymentMode modeEnum;
            if (groupEntities.Count > 1 || (totalCash > 0 && totalUpi > 0))
            {
                modeEnum = PaymentMode.Split;
            }
            else if (totalCash > 0)
            {
                modeEnum = PaymentMode.Cash;
            }
            else
            {
                modeEnum = PaymentMode.Upi;
            }

            bool isPreviousArrearsOnly = !string.IsNullOrWhiteSpace(primary.Notes) &&
                (primary.Notes.Contains("[Scope: PreviousArrears]", StringComparison.OrdinalIgnoreCase) ||
                 primary.Notes.Contains("PreviousArrears", StringComparison.OrdinalIgnoreCase) ||
                 primary.Notes.Contains("Previous Arrears", StringComparison.OrdinalIgnoreCase));

            bool isCurrentEventOnly = !string.IsNullOrWhiteSpace(primary.Notes) &&
                (primary.Notes.Contains("[Scope: CurrentEvent]", StringComparison.OrdinalIgnoreCase) ||
                 primary.Notes.Contains("CurrentEvent", StringComparison.OrdinalIgnoreCase) ||
                 primary.Notes.Contains("Current Event", StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                if (statusIdToAssign.HasValue) match.StatusId = statusIdToAssign.Value;
                if (primary.PaymentModeId.HasValue) match.PaymentModeId = primary.PaymentModeId.Value;
                match.PaymentDate = primary.PaymentDate != default ? primary.PaymentDate : DateTime.UtcNow;
                match.PaymentMode = modeEnum;

                // Mark contribution as paid if all verified OR member paid the full amount without rejection
                if (!isAnyRejected && (isAllVerified || isFullyCovered))
                {
                    match.PaymentStatus = PaymentStatus.Paid;
                }
                else
                {
                    match.PaymentStatus = PaymentStatus.Pending;
                }

                // CRITICAL: Submitted amounts must NEVER be lost or wiped when rejected.
                // Maintain the actual submitted cash & upi amounts.
                match.CashAmount = totalCash;
                match.UpiAmount = totalUpi;

                match.ModifiedBy = primary.ModifiedBy ?? primary.CreatedBy;
                match.ModifiedOn = DateTime.UtcNow;
                _contributionRepository.Update(match);
                _logger.LogInformation("Synchronized contribution {ContributionId} for transaction group: Status={Status}, Total={Amount}",
                    match.ContributionId, targetStatusName, totalCash + totalUpi);
            }
            else if (primary.EventId.HasValue && primary.UserId.HasValue)
            {
                var newContrib = new Contribution
                {
                    ContributionId = Guid.NewGuid(),
                    EventId = primary.EventId.Value,
                    UserId = primary.UserId.Value,
                    Amount = totalCash + totalUpi,
                    StatusId = statusIdToAssign,
                    PaymentModeId = primary.PaymentModeId,
                    PaymentDate = primary.PaymentDate != default ? primary.PaymentDate : DateTime.UtcNow,
                    CashAmount = totalCash,
                    UpiAmount = totalUpi,
                    PaymentStatus = (!isAnyRejected && (isAllVerified || (totalCash + totalUpi) > 0)) ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentMode = modeEnum,
                    CreatedBy = primary.CreatedBy,
                    CreatedAt = DateTime.UtcNow
                };
                await _contributionRepository.AddAsync(newContrib, cancellationToken);
                _logger.LogInformation("Created new contribution for member {Member} and event {Event}", primary.MemberName, primary.EventName);
            }

            // Multi-Event Settlement: allocate funds to member's other pending contributions based on payment scope
            var totalTransacted = totalCash + totalUpi;
            decimal remainingForOtherEvents;
            if (isCurrentEventOnly)
            {
                // When paying Current Event Only, do not spill funds to other events
                remainingForOtherEvents = 0;
            }
            else
            {
                var primaryAmount = match?.Amount ?? 0;
                remainingForOtherEvents = totalTransacted > primaryAmount ? totalTransacted - primaryAmount : 0;
            }

            if (primary.UserId.HasValue && remainingForOtherEvents > 0)
            {
                var allContribs = await _contributionRepository.GetAllAsync(cancellationToken);
                var otherMemberContribs = allContribs
                    .Where(c => c.MemberId == primary.UserId.Value &&
                                (match == null || c.ContributionId != match.ContributionId) &&
                                c.PaymentStatus != PaymentStatus.Paid)
                    .OrderBy(c => c.CreatedAt)
                    .ToList();

                foreach (var otherDto in otherMemberContribs)
                {
                    if (remainingForOtherEvents <= 0) break;

                    var otherEntity = await _contributionRepository.GetByEventAndMemberAsync(otherDto.EventId, otherDto.MemberId, cancellationToken);
                    if (otherEntity != null)
                    {
                        // STRICT RULE: No partial payments on events!
                        // A member's event can only be settled if remaining funds can FULLY cover the event's required amount.
                        if (otherEntity.Amount <= 0 || remainingForOtherEvents < otherEntity.Amount)
                        {
                            continue; // Skip this event, do NOT allocate partial amount!
                        }

                        var alloc = otherEntity.Amount;
                        remainingForOtherEvents -= alloc;

                        if (!isAnyRejected)
                        {
                            if (statusIdToAssign.HasValue) otherEntity.StatusId = statusIdToAssign.Value;
                            otherEntity.PaymentStatus = PaymentStatus.Paid;
                        }
                        else
                        {
                            otherEntity.PaymentStatus = PaymentStatus.Pending;
                        }

                        if (primary.PaymentModeId.HasValue) otherEntity.PaymentModeId = primary.PaymentModeId.Value;
                        otherEntity.PaymentDate = primary.PaymentDate != default ? primary.PaymentDate : DateTime.UtcNow;
                        otherEntity.PaymentMode = modeEnum;

                        if (modeEnum == PaymentMode.Cash)
                        {
                            otherEntity.CashAmount = (otherEntity.CashAmount ?? 0m) + alloc;
                        }
                        else
                        {
                            otherEntity.UpiAmount = (otherEntity.UpiAmount ?? 0m) + alloc;
                        }
                        otherEntity.ModifiedBy = primary.ModifiedBy ?? primary.CreatedBy;
                        otherEntity.ModifiedOn = DateTime.UtcNow;

                        _contributionRepository.Update(otherEntity);
                        _logger.LogInformation("Multi-event settled contribution {ContributionId} for member {MemberId}: Allocated={Alloc}, Remaining={Remaining}",
                            otherEntity.ContributionId, primary.UserId.Value, alloc, remainingForOtherEvents);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize contribution record for transaction group");
        }
    }

    private Task<string?> SaveScreenshotAsync(string? screenshotInput, string txnNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(screenshotInput))
        {
            return Task.FromResult<string?>(null);
        }

        // Store screenshot/receipt data directly in the database (Base64 data URI)
        var trimmed = screenshotInput.Trim();
        return Task.FromResult<string?>(trimmed);
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

