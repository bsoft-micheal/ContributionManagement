using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class PaymentTransactionService : IPaymentTransactionService
{
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentTransactionService(IPaymentTransactionRepository transactionRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<PaymentTransactionDto>> GetAllAsync(string? eventName = null, string? mode = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var list = await _transactionRepository.GetAllAsync(eventName, mode, status, startDate, endDate, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<PaymentTransactionDto>>(list);
    }

    public async Task<PaymentTransactionDto> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");
        return _mapper.Map<PaymentTransactionDto>(entity);
    }

    public async Task<PaymentTransactionDto> CreateAsync(CreatePaymentTransactionRequestDto request, string? user = null, CancellationToken cancellationToken = default)
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
            PaymentMode = string.IsNullOrWhiteSpace(request.PaymentMode) ? "UPI" : request.PaymentMode.Trim(),
            Utr = request.Utr?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim(),
            Notes = request.Notes?.Trim(),
            Screenshot = request.Screenshot?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? "System" : user,
            CreatedOn = DateTime.UtcNow
        };

        if (entity.Status == "Verified")
        {
            entity.VerifiedBy = string.IsNullOrWhiteSpace(user) ? "Admin" : user;
            entity.VerifiedOn = DateTime.UtcNow;
        }

        await _transactionRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PaymentTransactionDto>(entity);
    }

    public async Task<PaymentTransactionDto> VerifyAsync(Guid transactionId, VerifyPaymentRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");

        entity.Status = request.Status.Trim();
        entity.VerifiedBy = string.IsNullOrWhiteSpace(request.VerifiedBy) ? (string.IsNullOrWhiteSpace(user) ? "Admin" : user) : request.VerifiedBy.Trim();
        entity.VerifiedOn = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = request.Notes.Trim();
        }

        entity.ModifiedBy = entity.VerifiedBy;
        entity.ModifiedOn = DateTime.UtcNow;

        _transactionRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<PaymentTransactionDto>(entity);
    }

    public async Task DeleteAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var entity = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment transaction with ID {transactionId} not found.");

        _transactionRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
