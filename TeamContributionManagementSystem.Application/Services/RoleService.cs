using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoleService(IRoleRepository roleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<RoleDto>>(roles);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
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

    public async Task<RoleDto> UpdateAsync(Guid roleId, UpdateRoleRequestDto request, CancellationToken cancellationToken = default)
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
}
