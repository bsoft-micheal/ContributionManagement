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
            var userNames = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Users
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.FullName)
                .Select(x => new MemberDto
                {
                    MemberId = x.UserId,
                    Name = x.FullName,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.UserRoles.Select(ur => (Guid?)ur.RoleId).FirstOrDefault(),
                    RoleName = x.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member",
                    DefaultContributionAmount = 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && userNames.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && userNames.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
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
            var userNames = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Users
                .Where(x => !x.IsDeleted && x.IsActive && !x.IsExited)
                .OrderBy(x => x.FullName)
                .Select(x => new MemberDto
                {
                    MemberId = x.UserId,
                    Name = x.FullName,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.UserRoles.Select(ur => (Guid?)ur.RoleId).FirstOrDefault(),
                    RoleName = x.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member",
                    DefaultContributionAmount = 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && userNames.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && userNames.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
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
            var users = await _context.Users
                .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .Include(x => x.WorkTypeNavigation)
                .Where(x => ids.Contains(x.UserId) && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            return users.Select(user => new Member
            {
                MemberId = user.UserId,
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                Role = user.RoleNavigation,
                DateOfBirth = user.DateOfBirth,
                JoiningDate = user.JoiningDate,
                Gender = user.Gender,
                IsActive = user.IsActive,
                IsExited = user.IsExited,
                IsDeleted = user.IsDeleted,
                WorkType = user.WorkType,
                CreatedBy = user.CreatedBy,
                CreatedAt = user.CreatedAt,
                CreatedOn = user.CreatedOn,
                ModifiedBy = user.ModifiedBy,
                ModifiedOn = user.ModifiedOn
            }).ToList();
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
            var user = await _context.Users
                .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .Include(x => x.WorkTypeNavigation)
                .FirstOrDefaultAsync(x => x.UserId == memberId && !x.IsDeleted, cancellationToken);

            if (user == null) return null;

            return new Member
            {
                MemberId = user.UserId,
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                Role = user.RoleNavigation,
                DateOfBirth = user.DateOfBirth,
                JoiningDate = user.JoiningDate,
                Gender = user.Gender,
                IsActive = user.IsActive,
                IsExited = user.IsExited,
                IsDeleted = user.IsDeleted,
                WorkType = user.WorkType,
                CreatedBy = user.CreatedBy,
                CreatedAt = user.CreatedAt,
                CreatedOn = user.CreatedOn,
                ModifiedBy = user.ModifiedBy,
                ModifiedOn = user.ModifiedOn
            };
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
            var user = await _context.Users
                .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .Include(x => x.WorkTypeNavigation)
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower() && !x.IsDeleted, cancellationToken);

            if (user == null) return null;

            return new Member
            {
                MemberId = user.UserId,
                Name = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId,
                Role = user.RoleNavigation,
                DateOfBirth = user.DateOfBirth,
                JoiningDate = user.JoiningDate,
                Gender = user.Gender,
                IsActive = user.IsActive,
                IsExited = user.IsExited,
                IsDeleted = user.IsDeleted,
                WorkType = user.WorkType,
                CreatedBy = user.CreatedBy,
                CreatedAt = user.CreatedAt,
                CreatedOn = user.CreatedOn,
                ModifiedBy = user.ModifiedBy,
                ModifiedOn = user.ModifiedOn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetByEmailAsync));
            throw;
        }
    }

    public async Task<AppUser?> GetUserByNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.FullName.ToLower() == fullName.ToLower() && !x.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetUserByNameAsync));
            throw;
        }
    }

    public async Task<List<MemberDto>> GetActiveBirthdaysInMonthAsync(int month, CancellationToken cancellationToken = default)
    {
        try
        {
            var userNames = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Users
                .Where(x => !x.IsDeleted && x.IsActive && !x.IsExited && x.DateOfBirth.Month == month)
                .OrderBy(x => x.DateOfBirth.Day)
                .Select(x => new MemberDto
                {
                    MemberId = x.UserId,
                    Name = x.FullName,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.UserRoles.Select(ur => (Guid?)ur.RoleId).FirstOrDefault(),
                    RoleName = x.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member",
                    DefaultContributionAmount = 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && userNames.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && userNames.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetActiveBirthdaysInMonthAsync));
            throw;
        }
    }

    public async Task<List<MemberDto>> GetMembersWithoutUserAccountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userNames = await _context.Users
                .AsNoTracking()
                .Select(u => new { u.UserId, Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Username })
                .ToDictionaryAsync(u => u.UserId, u => u.Name, cancellationToken);

            var items = await _context.Users
                .Where(m => !m.IsDeleted && m.IsActive && string.IsNullOrEmpty(m.PasswordHash))
                .OrderBy(m => m.FullName)
                .Select(x => new MemberDto
                {
                    MemberId = x.UserId,
                    Name = x.FullName,
                    Email = x.Email,
                    Phone = x.Phone,
                    RoleId = x.UserRoles.Select(ur => (Guid?)ur.RoleId).FirstOrDefault(),
                    RoleName = x.UserRoles.Select(ur => ur.Role!.RoleName).FirstOrDefault() ?? "Member",
                    DefaultContributionAmount = 0,
                    DateOfBirth = x.DateOfBirth,
                    JoiningDate = x.JoiningDate,
                    Gender = x.Gender,
                    IsActive = x.IsActive,
                    IsExited = x.IsExited,
                    WorkType = x.WorkTypeNavigation != null ? x.WorkTypeNavigation.WorkTypeName : string.Empty,
                    CreatedBy = x.CreatedBy.HasValue ? x.CreatedBy.Value.ToString() : null,
                    CreatedAt = x.CreatedAt,
                    CreatedOn = x.CreatedOn,
                    ModifiedBy = x.ModifiedBy.HasValue ? x.ModifiedBy.Value.ToString() : null,
                    ModifiedOn = x.ModifiedOn
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.CreatedBy) && Guid.TryParse(item.CreatedBy, out var cGuid) && userNames.TryGetValue(cGuid, out var cName))
                {
                    item.CreatedBy = cName;
                }
                if (!string.IsNullOrWhiteSpace(item.ModifiedBy) && Guid.TryParse(item.ModifiedBy, out var mGuid) && userNames.TryGetValue(mGuid, out var mName))
                {
                    item.ModifiedBy = mName;
                }
            }

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMembersWithoutUserAccountAsync));
            throw;
        }
    }

    public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new AppUser
            {
                UserId = member.MemberId == Guid.Empty ? Guid.NewGuid() : member.MemberId,
                Username = member.Email.Split('@')[0],
                Email = member.Email,
                FullName = member.Name,
                Phone = member.Phone,
                Gender = member.Gender,
                WorkType = member.WorkType,
                RoleId = member.RoleId,
                DateOfBirth = member.DateOfBirth,
                JoiningDate = member.JoiningDate,
                IsActive = member.IsActive,
                IsExited = member.IsExited,
                IsDeleted = member.IsDeleted,
                CreatedBy = member.CreatedBy,
                CreatedAt = member.CreatedAt,
                CreatedOn = member.CreatedOn ?? DateTime.UtcNow
            };
            await _context.Users.AddAsync(user, cancellationToken);
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
            var existingUser = _context.Users.Local.FirstOrDefault(u => u.UserId == member.MemberId);
            if (existingUser != null)
            {
                existingUser.FullName = member.Name;
                existingUser.Email = member.Email;
                existingUser.Phone = member.Phone;
                existingUser.Gender = member.Gender;
                existingUser.WorkType = member.WorkType;
                existingUser.RoleId = member.RoleId;
                existingUser.DateOfBirth = member.DateOfBirth;
                existingUser.JoiningDate = member.JoiningDate;
                existingUser.IsActive = member.IsActive;
                existingUser.IsExited = member.IsExited;
                existingUser.IsDeleted = member.IsDeleted;
                existingUser.ModifiedBy = member.ModifiedBy;
                existingUser.ModifiedOn = member.ModifiedOn ?? DateTime.UtcNow;
            }
            else
            {
                var user = new AppUser
                {
                    UserId = member.MemberId,
                    FullName = member.Name,
                    Email = member.Email,
                    Phone = member.Phone,
                    Gender = member.Gender,
                    WorkType = member.WorkType,
                    RoleId = member.RoleId,
                    DateOfBirth = member.DateOfBirth,
                    JoiningDate = member.JoiningDate,
                    IsActive = member.IsActive,
                    IsExited = member.IsExited,
                    IsDeleted = member.IsDeleted,
                    ModifiedBy = member.ModifiedBy,
                    ModifiedOn = member.ModifiedOn ?? DateTime.UtcNow
                };
                _context.Users.Attach(user);
                _context.Entry(user).Property(x => x.FullName).IsModified = true;
                _context.Entry(user).Property(x => x.Email).IsModified = true;
                _context.Entry(user).Property(x => x.Phone).IsModified = true;
                _context.Entry(user).Property(x => x.Gender).IsModified = true;
                _context.Entry(user).Property(x => x.WorkTypeId).IsModified = true;
                _context.Entry(user).Property(x => x.DateOfBirth).IsModified = true;
                _context.Entry(user).Property(x => x.JoiningDate).IsModified = true;
                _context.Entry(user).Property(x => x.IsActive).IsModified = true;
                _context.Entry(user).Property(x => x.IsExited).IsModified = true;
                _context.Entry(user).Property(x => x.IsDeleted).IsModified = true;
                _context.Entry(user).Property(x => x.ModifiedBy).IsModified = true;
                _context.Entry(user).Property(x => x.ModifiedOn).IsModified = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(Update));
            throw;
        }
    }
}
