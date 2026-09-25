using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.Extensions.Logging.ILogger<MemberRepository> _logger;

    public MemberRepository(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<MemberRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Member>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    public async Task<List<Member>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllActiveAsync");
            throw;
        }
    }

    public async Task<List<Member>> GetByIdsAsync(IEnumerable<Guid> memberIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var ids = memberIds.ToList();
            return await _context.Members
                .Include(x => x.Role)
                .Where(x => ids.Contains(x.MemberId) && !x.IsDeleted)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdsAsync");
            throw;
        }
    }

    public async Task<Member?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.MemberId == memberId && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByIdAsync");
            throw;
        }
    }

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetByEmailAsync");
            throw;
        }
    }

    public async Task<List<Member>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
            .Include(x => x.Role)
            .Where(x => !x.IsDeleted && x.IsActive && x.DateOfBirth.Month == month)
            .OrderBy(x => x.DateOfBirth.Day)
            .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetActiveBirthdaysInMonthAsync");
            throw;
        }
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Members.AddAsync(member, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AddAsync");
            throw;
        }
    }

    public void Update(Member member)
    {
        try
        {
            _context.Members.Update(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Update");
            throw;
        }
    }
}
