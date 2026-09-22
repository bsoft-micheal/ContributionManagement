using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly Microsoft.Extensions.Logging.ILogger<RoleService> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoleService(Microsoft.Extensions.Logging.ILogger<RoleService> logger, IRoleRepository roleRepository, IUnitOfWork unitOfWork, IMapper mapper)
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
            var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<RoleDto>>(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _roleRepository.GetByNameAsync(request.RoleName.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("Role already exists.");
        }

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = request.RoleName.Trim(),
            DefaultContributionAmount = request.DefaultContributionAmount
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RoleDto>(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");

        var duplicate = await _roleRepository.GetByNameAsync(request.RoleName.Trim(), cancellationToken);
        if (duplicate is not null && duplicate.RoleId != roleId)
        {
            throw new InvalidOperationException("Role already exists.");
        }

        role.RoleName = request.RoleName.Trim();
        role.DefaultContributionAmount = request.DefaultContributionAmount;

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RoleDto>(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");

        if (await _roleRepository.HasMembersAsync(roleId, cancellationToken))
        {
            throw new InvalidOperationException("Cannot delete role because members are assigned to this role.");
        }

        _roleRepository.Delete(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}
