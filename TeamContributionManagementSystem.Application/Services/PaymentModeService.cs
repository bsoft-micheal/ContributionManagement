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
            return entity == null ? null : _mapper.Map<PaymentModeDto>(entity);
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
            if (existing != null)
            {
                throw new InvalidOperationException(string.Format(CommonMessages.PaymentModes.AlreadyExistsFormat, trimmedName));
            }

            var entity = new PaymentModeItem
            {
                PaymentModeId = Guid.NewGuid(),
                PaymentModeName = trimmedName,
                IsActive = request.IsActive,
                IsDeleted = false,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentModeDto>(entity);
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

            entity.PaymentModeName = trimmedName;
            entity.IsActive = request.IsActive;
            entity.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            entity.ModifiedOn = DateTime.UtcNow;

            _repository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<PaymentModeDto>(entity);
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

            _repository.Delete(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeletePaymentModeAsyncById));
            throw;
        }
    }
}
