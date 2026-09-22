using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<RoleRepository> _logger;

    public RoleRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles.OrderBy(x => x.RoleName).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.RoleName.ToLower() == roleName.ToLower(), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByNameAsync");
            throw;
        }
    }

    public async Task<bool> HasMembersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members.AnyAsync(x => x.RoleId == roleId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HasMembersAsync");
            throw;
        }
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Roles.AddAsync(role, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddAsync");
            throw;
        }
    }

    public void Update(Role role)
    {
        try
        {
            _context.Roles.Update(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            throw;
        }
    }

    public void Delete(Role role)
    {
        try
        {
            _context.Roles.Remove(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Delete");
            throw;
        }
    }
}
