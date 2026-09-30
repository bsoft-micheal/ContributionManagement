using System.IO;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Payments;
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
            var all = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
            var nextNum = 1250 + all.Count + 1;
            var txnNumber = $"{CommonConstants.Defaults.TxnPrefix}{nextNum:D6}";

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
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
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
            var all = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
            var nextNum = 1250 + all.Count + 1;
            var txnNumber = $"{CommonConstants.Defaults.TxnPrefix}{nextNum:D6}";

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

            // Resolve PaymentModeId
            Guid? resolvedPaymentModeId = null;
            var paymentModeName = string.IsNullOrWhiteSpace(request.PaymentMode) ? CommonConstants.PaymentModes.Upi : request.PaymentMode.Trim();
            if (_paymentModeRepository != null && !string.IsNullOrWhiteSpace(paymentModeName))
            {
                var matchedPm = await _paymentModeRepository.GetByNameAsync(paymentModeName, cancellationToken);
                resolvedPaymentModeId = matchedPm?.PaymentModeId;
            }

            // Resolve StatusId for Pending
            Guid? resolvedStatusId = null;
            if (_statusRepository != null)
            {
                var matchedSt = await _statusRepository.GetByNameAsync(CommonConstants.PaymentStatuses.Pending, cancellationToken);
                resolvedStatusId = matchedSt?.StatusId;
            }

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = resolvedMemberName,
                EventName = resolvedEventName,
                UserId = resolvedUserId,
                EventId = resolvedEventId,
                PaymentModeId = resolvedPaymentModeId,
                StatusId = resolvedStatusId,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate != default ? request.PaymentDate : DateTime.UtcNow,
                PaymentMode = paymentModeName,
                Utr = request.Utr?.Trim(),
                Status = CommonConstants.PaymentStatuses.Pending,
                Notes = request.Notes?.Trim(),
                Screenshot = await SaveScreenshotAsync(request.Screenshot, txnNumber, cancellationToken),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(resolvedMemberName) ? "System" : resolvedMemberName,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(entity, cancellationToken);
            await SyncContributionFromTransactionAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Payments.PaymentProofSubmitted,
                txnNumber, resolvedMemberName, resolvedEventName, request.Amount, request.Utr);

            return _mapper.Map<PaymentTransactionDto>(entity);
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

            // Load QR Settings
            try
            {
                var settings = await _settingService.GetSettingsAsync(cancellationToken);
                result.QrReceiverName = settings.QrReceiverName ?? CommonConstants.Defaults.DefaultPayeeName;
                result.QrUpiId = settings.QrUpiId ?? CommonConstants.Defaults.DefaultUpiId;
                result.QrImage = settings.QrImage ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, CommonLogMessages.Payments.SettingsLoadWarning);
                result.QrReceiverName = CommonConstants.Defaults.DefaultPayeeName;
                result.QrUpiId = CommonConstants.Defaults.DefaultUpiId;
            }

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
                    var isPaid = result.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase);
                    result.CurrentEventDue = isPaid ? 0 : contrib.Amount;
                }
            }

            // Calculate previous arrears
            if (!string.IsNullOrWhiteSpace(result.Email))
            {
                try
                {
                    var pastContribs = await _contributionRepository.GetByMemberEmailAsync(result.Email, cancellationToken);
                    var arrearsList = pastContribs
                        .Where(c => c.EventId != effectiveEventId && c.PaymentStatus != PaymentStatus.Paid)
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
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Payments.NotFound);

            entity.Status = request.Status.Trim();
            if (_statusRepository != null)
            {
                var matchedStatus = await _statusRepository.GetByNameAsync(entity.Status, cancellationToken);
                if (matchedStatus != null)
                {
                    entity.StatusId = matchedStatus.StatusId;
                }
            }

            entity.VerifiedBy = string.IsNullOrWhiteSpace(request.VerifiedBy) ? (string.IsNullOrWhiteSpace(user) ? null : user.Trim()) : request.VerifiedBy.Trim();
            entity.VerifiedOn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                entity.Notes = request.Notes.Trim();
            }

            entity.ModifiedBy = entity.VerifiedBy;
            entity.ModifiedOn = DateTime.UtcNow;

            _transactionRepository.Update(entity);
            await SyncContributionFromTransactionAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentTransactionDto>(entity);
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
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Payments.NotFound);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }

    private async Task SyncContributionFromTransactionAsync(PaymentTransaction entity, CancellationToken cancellationToken)
    {
        try
        {
            Contribution? match = null;
            if (entity.EventId.HasValue && entity.UserId.HasValue)
            {
                match = await _contributionRepository.GetByEventAndMemberAsync(entity.EventId.Value, entity.UserId.Value, cancellationToken);
            }

            if (match == null && !string.IsNullOrWhiteSpace(entity.MemberName) && !string.IsNullOrWhiteSpace(entity.EventName))
            {
                var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
                var matchDto = allContributions.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.MemberName) &&
                    c.MemberName.Trim().Equals(entity.MemberName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(c.EventName) &&
                    c.EventName.Trim().Equals(entity.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchDto != null)
                {
                    match = await _contributionRepository.GetByEventAndMemberAsync(matchDto.EventId, matchDto.MemberId, cancellationToken);
                }
            }

            var isVerified = entity.Status.Equals(CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase);
            var targetStatusName = isVerified ? "Paid" : "Pending";

            Guid? statusIdToAssign = null;
            if (_statusRepository != null)
            {
                var st = await _statusRepository.GetByNameAsync(targetStatusName, cancellationToken);
                statusIdToAssign = st?.StatusId;
            }

            if (match != null)
            {
                if (statusIdToAssign.HasValue) match.StatusId = statusIdToAssign.Value;
                if (entity.PaymentModeId.HasValue) match.PaymentModeId = entity.PaymentModeId.Value;
                match.PaymentDate = entity.PaymentDate != default ? entity.PaymentDate : DateTime.UtcNow;

                var modeLower = (entity.PaymentMode ?? "").ToLowerInvariant();
                var isSplit = modeLower.Contains("split");
                var isCash = string.Equals(entity.PaymentMode, "Cash", StringComparison.OrdinalIgnoreCase);

                if (isSplit)
                {
                    match.PaymentMode = PaymentMode.Split;
                    decimal cashPart = 0;
                    decimal upiPart = 0;
                    if (!string.IsNullOrWhiteSpace(entity.Notes) && entity.Notes.Contains("Cash:"))
                    {
                        var parts = entity.Notes.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (part.Contains("Cash:", StringComparison.OrdinalIgnoreCase))
                            {
                                var numStr = System.Text.RegularExpressions.Regex.Match(part, @"\d+(\.\d+)?").Value;
                                if (decimal.TryParse(numStr, out var cVal)) cashPart += cVal;
                            }
                            else
                            {
                                var numStr = System.Text.RegularExpressions.Regex.Match(part, @"\d+(\.\d+)?").Value;
                                if (decimal.TryParse(numStr, out var uVal)) upiPart += uVal;
                            }
                        }
                    }
                    if (cashPart == 0 && upiPart == 0) upiPart = entity.Amount;
                    match.CashAmount = cashPart;
                    match.UpiAmount = upiPart;
                }
                else if (isCash)
                {
                    match.CashAmount = entity.Amount;
                    match.UpiAmount = 0;
                    match.PaymentMode = PaymentMode.Cash;
                }
                else
                {
                    match.UpiAmount = entity.Amount;
                    match.CashAmount = 0;
                    if (Enum.TryParse<PaymentMode>(entity.PaymentMode, true, out var parsedMode))
                    {
                        match.PaymentMode = parsedMode;
                    }
                    else
                    {
                        match.PaymentMode = PaymentMode.Upi;
                    }
                }

                match.PaymentStatus = isVerified ? PaymentStatus.Paid : PaymentStatus.Pending;
                match.ModifiedBy = entity.VerifiedBy ?? entity.ModifiedBy ?? entity.CreatedBy;
                match.ModifiedOn = DateTime.UtcNow;
                _contributionRepository.Update(match);
                _logger.LogInformation(CommonLogMessages.Payments.ContributionSyncSuccess, match.ContributionId, targetStatusName, entity.MemberName, entity.EventName);
            }
            else if (entity.EventId.HasValue && entity.UserId.HasValue)
            {
                var modeLower = (entity.PaymentMode ?? "").ToLowerInvariant();
                var isSplit = modeLower.Contains("split");
                var isCash = string.Equals(entity.PaymentMode, "Cash", StringComparison.OrdinalIgnoreCase);

                decimal cashAmount = isCash ? entity.Amount : 0;
                decimal upiAmount = (!isCash && !isSplit) ? entity.Amount : 0;
                var modeEnum = PaymentMode.Upi;

                if (isSplit)
                {
                    modeEnum = PaymentMode.Split;
                    if (!string.IsNullOrWhiteSpace(entity.Notes) && entity.Notes.Contains("Cash:"))
                    {
                        var parts = entity.Notes.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            if (part.Contains("Cash:", StringComparison.OrdinalIgnoreCase))
                            {
                                var numStr = System.Text.RegularExpressions.Regex.Match(part, @"\d+(\.\d+)?").Value;
                                if (decimal.TryParse(numStr, out var cVal)) cashAmount += cVal;
                            }
                            else
                            {
                                var numStr = System.Text.RegularExpressions.Regex.Match(part, @"\d+(\.\d+)?").Value;
                                if (decimal.TryParse(numStr, out var uVal)) upiAmount += uVal;
                            }
                        }
                    }
                    if (cashAmount == 0 && upiAmount == 0) upiAmount = entity.Amount;
                }
                else if (isCash)
                {
                    modeEnum = PaymentMode.Cash;
                }
                else if (Enum.TryParse<PaymentMode>(entity.PaymentMode, true, out var parsed))
                {
                    modeEnum = parsed;
                }

                var newContrib = new Contribution
                {
                    ContributionId = Guid.NewGuid(),
                    EventId = entity.EventId.Value,
                    UserId = entity.UserId.Value,
                    Amount = entity.Amount,
                    StatusId = statusIdToAssign,
                    PaymentModeId = entity.PaymentModeId,
                    PaymentDate = entity.PaymentDate != default ? entity.PaymentDate : DateTime.UtcNow,
                    CashAmount = cashAmount,
                    UpiAmount = upiAmount,
                    PaymentStatus = isVerified ? PaymentStatus.Paid : PaymentStatus.Pending,
                    PaymentMode = modeEnum,
                    CreatedBy = entity.CreatedBy ?? "System",
                    CreatedAt = DateTime.UtcNow
                };
                await _contributionRepository.AddAsync(newContrib, cancellationToken);
                _logger.LogInformation("Created and synchronized new contribution record for {Member} - {Event}", entity.MemberName, entity.EventName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to synchronize contribution record for payment transaction: {TxnNumber}", entity.TxnNumber);
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
}

