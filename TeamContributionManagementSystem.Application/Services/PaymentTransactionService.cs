using Microsoft.Extensions.Logging;
using AutoMapper;
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
            var list = await _transactionRepository.GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
            return _mapper.Map<IReadOnlyCollection<PaymentTransactionDto>>(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<PaymentTransactionDto> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");
            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _transactionRepository.GetAllAsync(cancellationToken: cancellationToken);
            var nextNum = 1250 + all.Count + 1;
            var txnNumber = $"TXN{nextNum:D6}";

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = request.MemberName.Trim(),
                EventName = request.EventName.Trim(),
                Amount = request.Amount,
                PaymentDate = request.PaymentDate,
                PaymentMode = request.PaymentMode?.Trim() ?? "UPI",
                Utr = request.Utr?.Trim(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim(),
                Notes = request.Notes?.Trim(),
                Screenshot = request.Screenshot?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (entity.Status == "Verified")
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
            _logger.LogError(ex, "Error in CreateAsync");
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
            var txnNumber = $"TXN{nextNum:D6}";

            var entity = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                TxnNumber = txnNumber,
                MemberName = resolvedMemberName,
                EventName = resolvedEventName,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate != default ? request.PaymentDate : DateTime.UtcNow,
                PaymentMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? "UPI" : request.PaymentMode.Trim(),
                Utr = request.Utr?.Trim(),
                Status = "Pending",
                Notes = request.Notes?.Trim(),
                Screenshot = request.Screenshot?.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedBy = resolvedMemberName,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New Payment proof submitted: TxnNumber {TxnNumber}, Member {MemberName}, Event {EventName}, Amount {Amount}, UTR {Utr}",
                txnNumber, resolvedMemberName, resolvedEventName, request.Amount, request.Utr);

            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SubmitProofAsync");
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
                result.QrReceiverName = settings.QrReceiverName ?? "Daniel A";
                result.QrUpiId = settings.QrUpiId ?? "danielrobertanto604@okicici";
                result.QrImage = settings.QrImage ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch settings for payment context; using defaults");
                result.QrReceiverName = "Daniel A";
                result.QrUpiId = "danielrobertanto604@okicici";
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
            _logger.LogError(ex, "Error in GetPaymentContextAsync");
            throw;
        }
    }

    public async Task<PaymentTransactionDto> VerifyAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");

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
                var match = allContributions.FirstOrDefault(c =>
                    !c.IsDeleted &&
                    c.Member != null && !string.IsNullOrWhiteSpace(c.Member.Name) &&
                    c.Member.Name.Trim().Equals(entity.MemberName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    c.Event != null && !string.IsNullOrWhiteSpace(c.Event.EventName) &&
                    c.Event.EventName.Trim().Equals(entity.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    if (entity.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
                    {
                        match.PaymentStatus = PaymentStatus.Paid;
                        match.PaymentDate = entity.PaymentDate != default ? entity.PaymentDate : DateTime.UtcNow;
                        match.PaymentMode = Enum.TryParse<PaymentMode>(entity.PaymentMode, true, out var pm) ? pm : PaymentMode.Upi;
                        match.UpiAmount = entity.Amount;
                        match.ModifiedBy = entity.VerifiedBy ?? user;
                        match.ModifiedOn = DateTime.UtcNow;
                        _contributionRepository.Update(match);
                        _logger.LogInformation("Synchronized Contribution {ContributionId} to Paid for Member {Member} on Event {Event}", match.ContributionId, entity.MemberName, entity.EventName);
                    }
                    else if (entity.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase) || entity.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) || entity.Status.Equals("Needs Clarification", StringComparison.OrdinalIgnoreCase))
                    {
                        match.PaymentStatus = PaymentStatus.Pending;
                        match.UpiAmount = 0;
                        match.ModifiedBy = entity.VerifiedBy ?? user;
                        match.ModifiedOn = DateTime.UtcNow;
                        _contributionRepository.Update(match);
                        _logger.LogInformation("Synchronized Contribution {ContributionId} to Pending for Member {Member} on Event {Event}", match.ContributionId, entity.MemberName, entity.EventName);
                    }
                }
            }
            catch (Exception syncEx)
            {
                _logger.LogWarning(syncEx, "Failed to auto-sync contribution status during payment verification for Txn: {TxnNumber}", entity.TxnNumber);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentTransactionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");

            _transactionRepository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}

