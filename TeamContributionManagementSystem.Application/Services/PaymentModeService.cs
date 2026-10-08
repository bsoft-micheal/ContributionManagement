using AutoMapper;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.PaymentModes;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class PaymentModeService : IPaymentModeService
{
    private readonly IPaymentModeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentModeService> _logger;
    private readonly IUserRepository? _userRepository;

    public PaymentModeService(
        IPaymentModeRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PaymentModeService> logger,
        IUserRepository? userRepository = null)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<PaymentModeDto>> GetAllPaymentModeAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _repository.GetAllAsync(activeOnly, cancellationToken);
            var dtos = _mapper.Map<List<PaymentModeDto>>(entities);

            if (_userRepository != null)
            {
                try
                {
                    var users = await _userRepository.GetAllAsync(cancellationToken);
                    var userDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var u in users)
                    {
                        var displayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username;
                        userDict[u.UserId.ToString()] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.Username)) userDict[u.Username] = displayName;
                        if (!string.IsNullOrWhiteSpace(u.FullName)) userDict[u.FullName] = displayName;
                    }

                    foreach (var dto in dtos)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && userDict.TryGetValue(dto.CreatedBy, out var createdByName))
                        {
                            dto.CreatedBy = createdByName;
                        }
                        if (!string.IsNullOrWhiteSpace(dto.ModifiedBy) && userDict.TryGetValue(dto.ModifiedBy, out var updatedByName))
                        {
                            dto.ModifiedBy = updatedByName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resolve user names for payment modes");
                }
            }

            foreach (var dto in dtos)
            {
                var matchingEntity = entities.FirstOrDefault(e => e.PaymentModeId == dto.PaymentModeId);
                EnrichPaymentMode(dto, matchingEntity);
                dto.IsReferred = await _repository.IsInUseAsync(dto.PaymentModeName, cancellationToken);
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllPaymentModeAsync));
            throw;
        }
    }

    public async Task<PaymentModeDto?> GetPaymentModeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null) return null;
            var dto = _mapper.Map<PaymentModeDto>(entity);
            EnrichPaymentMode(dto, entity);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetPaymentModeAsyncById));
            throw;
        }
    }

    public async Task<PaymentModeDto> SavePaymentModeAsync(CreatePaymentModeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedName = request.PaymentModeName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            var isCash = request.IsCash ?? (string.Equals(trimmedName, "Cash", StringComparison.OrdinalIgnoreCase) || trimmedName.IndexOf("cash", StringComparison.OrdinalIgnoreCase) >= 0);
            var isSplit = string.Equals(trimmedName, "Split", StringComparison.OrdinalIgnoreCase) || trimmedName.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0;
            var supportsQr = request.SupportsQr ?? !isCash;
            var paymentType = !string.IsNullOrWhiteSpace(request.PaymentType)
                ? request.PaymentType
                : (isCash ? "Cash" : (isSplit ? "Split" : "Digital"));

            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = request.IsActive;
                    existing.IsCash = isCash;
                    existing.SupportsQr = supportsQr;
                    existing.PaymentType = paymentType;
                    existing.ModifiedBy = CommonMethods.ParseNullableGuid(user);
                    existing.ModifiedOn = DateTime.UtcNow;

                    _repository.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    var restoredDto = _mapper.Map<PaymentModeDto>(existing);
                    EnrichPaymentMode(restoredDto, existing);
                    return restoredDto;
                }

                throw new InvalidOperationException(string.Format(CommonMessages.PaymentModes.AlreadyExistsFormat, trimmedName));
            }

            var entity = new PaymentModeItem
            {
                PaymentModeId = Guid.NewGuid(),
                PaymentModeName = trimmedName,
                IsActive = request.IsActive,
                IsDeleted = false,
                IsCash = isCash,
                SupportsQr = supportsQr,
                PaymentType = paymentType,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<PaymentModeDto>(entity);
            EnrichPaymentMode(dto, entity);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(SavePaymentModeAsync));
            throw;
        }
    }

    public async Task<PaymentModeDto> UpdatePaymentModeAsyncById(Guid id, UpdatePaymentModeRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.PaymentModes.NotFoundFormat, id));

            var trimmedName = request.PaymentModeName.Trim();
            var existing = await _repository.GetByNameAsync(trimmedName, cancellationToken);
            if (existing != null && existing.PaymentModeId != id)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.PaymentModes.AlreadyExistsFormat, trimmedName));
            }

            var isCash = request.IsCash ?? (string.Equals(trimmedName, "Cash", StringComparison.OrdinalIgnoreCase) || trimmedName.IndexOf("cash", StringComparison.OrdinalIgnoreCase) >= 0);
            var isSplit = string.Equals(trimmedName, "Split", StringComparison.OrdinalIgnoreCase) || trimmedName.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0;
            var supportsQr = request.SupportsQr ?? !isCash;
            var paymentType = !string.IsNullOrWhiteSpace(request.PaymentType)
                ? request.PaymentType
                : (isCash ? "Cash" : (isSplit ? "Split" : "Digital"));

            entity.PaymentModeName = trimmedName;
            entity.IsActive = request.IsActive;
            entity.IsCash = isCash;
            entity.SupportsQr = supportsQr;
            entity.PaymentType = paymentType;
            entity.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            entity.ModifiedOn = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = _mapper.Map<PaymentModeDto>(entity);
            EnrichPaymentMode(dto, entity);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdatePaymentModeAsyncById));
            throw;
        }
    }

    public async Task DeletePaymentModeAsyncById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(CommonMessages.PaymentModes.NotFoundFormat, id));

            if (await _repository.IsInUseAsync(entity.PaymentModeName, cancellationToken))
            {
                throw new InvalidOperationException(CommonMessages.General.RecordInUse);
            }

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeletePaymentModeAsyncById));
            throw;
        }
    }

    private static void EnrichPaymentMode(PaymentModeDto dto, PaymentModeItem? entity = null)
    {
        var name = dto.PaymentModeName?.Trim() ?? string.Empty;
        var isCashName = string.Equals(name, "Cash", StringComparison.OrdinalIgnoreCase) || name.IndexOf("cash", StringComparison.OrdinalIgnoreCase) >= 0;
        var isSplitName = string.Equals(name, "Split", StringComparison.OrdinalIgnoreCase) || name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0;

        bool isCash = entity != null ? entity.IsCash : isCashName;
        bool supportsQr = entity != null ? entity.SupportsQr : (!isCashName);
        string paymentType = !string.IsNullOrWhiteSpace(entity?.PaymentType)
            ? entity.PaymentType!
            : (isCash ? "Cash" : (isSplitName ? "Split" : "Digital"));

        dto.IsCash = isCash;
        dto.SupportsQr = supportsQr;
        dto.PaymentType = paymentType;
    }
}
