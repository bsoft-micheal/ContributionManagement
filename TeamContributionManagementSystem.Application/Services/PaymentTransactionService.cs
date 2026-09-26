using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Payments;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentTransactionService(
        ILogger<PaymentTransactionService> logger,
        IPaymentTransactionRepository transactionRepository,
        IContributionRepository contributionRepository,
        IEventRepository eventRepository,
        IMemberRepository memberRepository,
        ISystemSettingService settingService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _contributionRepository = contributionRepository;
        _eventRepository = eventRepository;
        _memberRepository = memberRepository;
        _settingService = settingService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _transactionRepository.GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
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

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = request.MemberName.Trim(),
                EventName = request.EventName.Trim(),
                Amount = request.Amount,
                PaymentDate = request.PaymentDate,
                PaymentMode = request.PaymentMode?.Trim() ?? CommonConstants.PaymentModes.Upi,
                Utr = request.Utr?.Trim(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? CommonConstants.PaymentStatuses.Pending : request.Status.Trim(),
                Notes = request.Notes?.Trim(),
                Screenshot = request.Screenshot?.Trim(),
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
            string resolvedEventName = request.EventName?.Trim() ?? string.Empty;
            string resolvedMemberName = request.MemberName?.Trim() ?? string.Empty;

            if (request.EventId.HasValue && request.EventId.Value != Guid.Empty && string.IsNullOrWhiteSpace(resolvedEventName))
            {
                var ev = await _eventRepository.GetByIdAsync(request.EventId.Value, cancellationToken);
                if (ev != null) resolvedEventName = ev.EventName;
            }

            if (request.MemberId.HasValue && request.MemberId.Value != Guid.Empty && string.IsNullOrWhiteSpace(resolvedMemberName))
            {
                var mem = await _memberRepository.GetByIdAsync(request.MemberId.Value, cancellationToken);
                if (mem != null) resolvedMemberName = mem.Name;
            }

            var all = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
            var nextNum = 1250 + all.Count + 1;
            var txnNumber = $"{CommonConstants.Defaults.TxnPrefix}{nextNum:D6}";

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = resolvedMemberName,
                EventName = resolvedEventName,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate != default ? request.PaymentDate : DateTime.UtcNow,
                PaymentMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? CommonConstants.PaymentModes.Upi : request.PaymentMode.Trim(),
                Utr = request.Utr?.Trim(),
                Status = CommonConstants.PaymentStatuses.Pending,
                Notes = request.Notes?.Trim(),
                Screenshot = request.Screenshot?.Trim(),
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
                var allContributions = await _contributionRepository.GetAllAsync(cancellationToken);
                var matchDto = allContributions.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.MemberName) &&
                    c.MemberName.Trim().Equals(entity.MemberName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(c.EventName) &&
                    c.EventName.Trim().Equals(entity.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchDto != null)
                {
                    var match = await _contributionRepository.GetByEventAndMemberAsync(matchDto.EventId, matchDto.MemberId, cancellationToken);
                    if (match != null)
                    {
                        if (entity.Status.Equals(CommonConstants.PaymentStatuses.Verified, StringComparison.OrdinalIgnoreCase))
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
                        else if (entity.Status.Equals(CommonConstants.PaymentStatuses.Pending, StringComparison.OrdinalIgnoreCase) || 
                                 entity.Status.Equals(CommonConstants.PaymentStatuses.Failed, StringComparison.OrdinalIgnoreCase) || 
                                 entity.Status.Equals(CommonConstants.PaymentStatuses.NeedsClarification, StringComparison.OrdinalIgnoreCase))
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
}

