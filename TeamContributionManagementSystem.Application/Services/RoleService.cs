using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly ILogger<RoleService> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoleService(ILogger<RoleService> logger, IRoleRepository roleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _roleRepository.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _roleRepository.GetByNameAsync(request.RoleName.Trim(), cancellationToken);
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = true;
                    existing.DefaultContributionAmount = request.DefaultContributionAmount;
                    existing.ModifiedBy = CommonMethods.ParseNullableGuid(user);
                    existing.ModifiedOn = DateTime.UtcNow;

                    _roleRepository.Update(existing);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return _mapper.Map<RoleDto>(existing);
                }

                throw new InvalidOperationException(CommonMessages.Roles.AlreadyExists);
            }

            var role = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = request.RoleName.Trim(),
                DefaultContributionAmount = request.DefaultContributionAmount,
                CreatedBy = CommonMethods.ParseNullableGuid(user),
                CreatedAt = DateTime.UtcNow
            };

            await _roleRepository.AddAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<RoleDto>(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Roles.NotFound);

            var duplicate = await _roleRepository.GetByNameAsync(request.RoleName.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.RoleId != roleId)
            {
                throw new InvalidOperationException(CommonMessages.Roles.AlreadyExists);
            }

            role.RoleName = request.RoleName.Trim();
            role.DefaultContributionAmount = request.DefaultContributionAmount;
            role.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            role.ModifiedOn = DateTime.UtcNow;

            _roleRepository.Update(role);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<RoleDto>(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Roles.NotFound);

            var nameLower = role.RoleName.Trim().ToLower();
            if (nameLower == "admin" || nameLower == "member")
            {
                throw new InvalidOperationException("Core system roles (Admin and Member) cannot be deleted.");
            }

            await _roleRepository.DeleteRoleWithAssignmentsAsync(roleId, role.RoleName, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
