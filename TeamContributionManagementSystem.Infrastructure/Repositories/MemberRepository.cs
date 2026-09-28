using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Infrastructure.Persistence;

namespace TeamContributionManagementSystem.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MemberRepository> _logger;

    public MemberRepository(ApplicationDbContext context, ILogger<MemberRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new MemberDto
                {
                    MemberId = x.MemberId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.RoleId,
                    RoleName = x.Role != null ? x.Role.RoleName : string.Empty,
                    DefaultContributionAmount = x.Role != null ? x.Role.DefaultContributionAmount : 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    MemberType = x.MemberType,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<List<MemberDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new MemberDto
                {
                    MemberId = x.MemberId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.RoleId,
                    RoleName = x.Role != null ? x.Role.RoleName : string.Empty,
                    DefaultContributionAmount = x.Role != null ? x.Role.DefaultContributionAmount : 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    MemberType = x.MemberType,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllActiveAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdsAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByIdAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEmailAsync));
            throw;
        }
    }

    public async Task<List<MemberDto>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Members
                .Where(x => !x.IsDeleted && x.IsActive && x.DateOfBirth.Month == month)
                .OrderBy(x => x.DateOfBirth.Day)
                .Select(x => new MemberDto
                {
                    MemberId = x.MemberId,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.RoleId,
                    RoleName = x.Role != null ? x.Role.RoleName : string.Empty,
                    DefaultContributionAmount = x.Role != null ? x.Role.DefaultContributionAmount : 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    MemberType = x.MemberType,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetActiveBirthdaysInMonthAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(AddAsync));
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
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }
}
