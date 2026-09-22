using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class RoleRightsService : IRoleRightsService
{
    private readonly Microsoft.Extensions.Logging.ILogger<RoleRightsService> _logger;
    private readonly IRoleRightRepository _roleRightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoleRightsService(Microsoft.Extensions.Logging.ILogger<RoleRightsService> logger, 
        IRoleRightRepository roleRightRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _logger = logger;
        _roleRightRepository = roleRightRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<RoleRightDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rights = await _roleRightRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<RoleRightDto>>(rights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<RoleRightDto>> GetByRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var role))
        {
            throw new ArgumentException($"Invalid role: '{roleName}'");
        }

        var rights = await _roleRightRepository.GetByRoleAsync(role, cancellationToken);
        return _mapper.Map<IReadOnlyCollection<RoleRightDto>>(rights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByRoleAsync");
            throw;
        }
    }

    public async Task SaveRoleRightsAsync(UpdateRoleRightsRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Enum.TryParse<UserRole>(request.RoleName, ignoreCase: true, out var role))
        {
            throw new ArgumentException($"Invalid role: '{request.RoleName}'");
        }

        var entities = request.Rights.Select(r => new RoleRight
        {
            RoleRightId = Guid.NewGuid(),
            Role = role,
            Module = r.Module,
            SubModule = r.SubModule,
            Page = r.Page,
            Access = r.Access
        }).ToList();

        await _roleRightRepository.SaveRoleRightsAsync(role, entities, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SaveRoleRightsAsync");
            throw;
        }
    }
}
