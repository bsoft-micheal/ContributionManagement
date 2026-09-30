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

            // Resolve EventId — prefer request.EventId, fall back to lookup by name
            Guid? resolvedEventId = null;
            string resolvedEventName = request.EventName?.Trim() ?? string.Empty;
            if (request.EventId.HasValue && request.EventId.Value != Guid.Empty)
            {
                resolvedEventId = request.EventId.Value;
                if (string.IsNullOrWhiteSpace(resolvedEventName))
                {
                    var ev = await _eventRepository.GetByIdAsync(request.EventId.Value, cancellationToken);
                    if (ev != null) resolvedEventName = ev.EventName;
                }
            }
            else if (!string.IsNullOrWhiteSpace(resolvedEventName))
            {
                var ev = await _eventRepository.GetByNameAsync(resolvedEventName, cancellationToken);
                resolvedEventId = ev?.EventId;
            }

            // Resolve UserId — prefer request.MemberId, fall back to lookup by name
            Guid? resolvedUserId = null;
            string resolvedMemberName = request.MemberName?.Trim() ?? string.Empty;
            if (request.MemberId.HasValue && request.MemberId.Value != Guid.Empty)
            {
                resolvedUserId = request.MemberId.Value;
                if (string.IsNullOrWhiteSpace(resolvedMemberName))
                {
                    var mem = await _memberRepository.GetByIdAsync(request.MemberId.Value, cancellationToken);
                    if (mem != null) resolvedMemberName = mem.Name;
                }
            }
            else if (!string.IsNullOrWhiteSpace(resolvedMemberName))
            {
                var mem = await _memberRepository.GetUserByNameAsync(resolvedMemberName, cancellationToken);
                resolvedUserId = mem?.UserId;
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
                CreatedBy = resolvedMemberName,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(entity, cancellationToken);
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

    public async Task<PaymentContextDto?> GetPaymentContextAsync(Guid? eventId, Guid? memberId, CancellationToken cancellationToken = default)
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

            if (eventId.HasValue && eventId.Value != Guid.Empty)
            {
                var ev = await _eventRepository.GetByIdAsync(eventId.Value, cancellationToken);
                if (ev != null)
                {
                    result.EventId = ev.EventId;
                    result.EventName = ev.EventName;
                }
            }

            if (memberId.HasValue && memberId.Value != Guid.Empty)
            {
                var mem = await _memberRepository.GetByIdAsync(memberId.Value, cancellationToken);
                if (mem != null)
                {
                    result.MemberId = mem.MemberId;
                    result.MemberName = mem.Name;
                    result.Email = mem.Email;
                }
            }

            // Check specific contribution record
            if (eventId.HasValue && memberId.HasValue && eventId.Value != Guid.Empty && memberId.Value != Guid.Empty)
            {
                var contrib = await _contributionRepository.GetByEventAndMemberAsync(eventId.Value, memberId.Value, cancellationToken);
                if (contrib != null)
                {
                    result.Amount = contrib.Amount;
                    result.Status = contrib.PaymentStatus.ToString();
                }
            }

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

            // Synchronize with Contribution entity
            try
            {
                Contribution? match = null;
                if (entity.EventId.HasValue && entity.UserId.HasValue)
                {
                    match = await _contributionRepository.GetByEventAndMemberAsync(entity.EventId.Value, entity.UserId.Value, cancellationToken);
                }

                if (match == null)
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

                if (match != null)
                {
                    var isVerified = entity.Status.Equals(CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase);
                    var targetStatusName = isVerified ? "Paid" : "Pending";

                    if (_statusRepository != null)
                    {
                        var st = await _statusRepository.GetByNameAsync(targetStatusName, cancellationToken);
                        if (st != null)
                        {
                            match.StatusId = st.StatusId;
                        }
                    }

                    if (entity.PaymentModeId.HasValue)
                    {
                        match.PaymentModeId = entity.PaymentModeId.Value;
                    }

                    if (isVerified)
                    {
                        match.PaymentStatus = PaymentStatus.Paid;
                        match.PaymentDate = entity.PaymentDate != default ? entity.PaymentDate : DateTime.UtcNow;
                        match.PaymentMode = Enum.TryParse<PaymentMode>(entity.PaymentMode, true, out var pm) ? pm : PaymentMode.Upi;
                        match.UpiAmount = entity.Amount;
                        match.ModifiedBy = entity.VerifiedBy ?? user;
                        match.ModifiedOn = DateTime.UtcNow;
                        _contributionRepository.Update(match);
                        _logger.LogInformation(CommonLogMessages.Payments.ContributionSyncSuccess, match.ContributionId, CommonConstants.PaymentStatuses.Paid, entity.MemberName, entity.EventName);
                    }
                    else
                    {
                        match.PaymentStatus = PaymentStatus.Pending;
                        match.UpiAmount = 0;
                        match.ModifiedBy = entity.VerifiedBy ?? user;
                        match.ModifiedOn = DateTime.UtcNow;
                        _contributionRepository.Update(match);
                        _logger.LogInformation(CommonLogMessages.Payments.ContributionSyncSuccess, match.ContributionId, CommonConstants.PaymentStatuses.Pending, entity.MemberName, entity.EventName);
                    }
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, CommonLogMessages.Payments.ContributionSyncFailed, entity.TxnNumber);
            }

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
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
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

