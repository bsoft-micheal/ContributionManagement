using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ILogger<UserManagementService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;
    private readonly IWorkTypeRepository? _workTypeRepository;
    private readonly IEmailService _emailService;

    public UserManagementService(ILogger<UserManagementService> logger, 
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IMapper mapper,
        IEmailService emailService,
        IWorkTypeRepository? workTypeRepository = null)
    {
        _logger = logger;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
        _workTypeRepository = workTypeRepository;
        _emailService = emailService;
    }

    private async Task<string> ResolveDefaultWorkTypeAsync(CancellationToken cancellationToken)
    {
        if (_workTypeRepository != null)
        {
            var active = await _workTypeRepository.GetAllAsync(true, cancellationToken);
            var first = active.FirstOrDefault()?.WorkTypeName;
            if (!string.IsNullOrWhiteSpace(first))
                return first.Trim();
        }
        return "Office";
    }

    private async Task<Guid?> ResolveWorkTypeIdAsync(string? workTypeName, CancellationToken cancellationToken)
    {
        if (_workTypeRepository != null && !string.IsNullOrWhiteSpace(workTypeName))
        {
            var matched = await _workTypeRepository.GetByNameAsync(workTypeName.Trim(), cancellationToken);
            if (matched != null) return matched.WorkTypeId;

            var all = await _workTypeRepository.GetAllAsync(true, cancellationToken);
            var first = all.FirstOrDefault();
            if (first != null) return first.WorkTypeId;
        }
        return null;
    }

    // ── HELPER: ENRICH USER DTO WITH MERGED PROFILE ──────────────────────────
    private async Task<UserDto> EnrichUserDtoWithMemberProfileAsync(AppUser user, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<UserDto>(user);
        dto.IsFirstLogin = user.IsFirstLogin;
        dto.HasMemberProfile = true;
        dto.MemberUsername = user.FullName;
        dto.DateOfBirth = user.DateOfBirth;
        dto.JoiningDate = user.JoiningDate;
        dto.Gender = user.Gender;
        dto.Phone = user.Phone;
        dto.WorkType = !string.IsNullOrWhiteSpace(user.WorkType)
            ? user.WorkType
            : await ResolveDefaultWorkTypeAsync(cancellationToken);

        var activeUserRoles = user.UserRoles?.ToList() ?? new List<AppUserRole>();
        var primaryRoleNames = activeUserRoles.Where(ur => ur.IsPrimary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var secondaryRoleNames = activeUserRoles.Where(ur => ur.IsSecondary && ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var allRoleNames = activeUserRoles.Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.RoleName)).Select(ur => ur.Role!.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var primaryRoleIds = activeUserRoles.Where(ur => ur.IsPrimary).Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();
        var secondaryRoleIds = activeUserRoles.Where(ur => ur.IsSecondary).Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();
        var allRoleIds = activeUserRoles.Select(ur => ur.RoleId).Where(id => id != Guid.Empty).Distinct().ToList();

        Guid? activeRoleId = (user.RoleId.HasValue && user.RoleId.Value != Guid.Empty)
            ? user.RoleId
            : (primaryRoleIds.Count > 0
                ? primaryRoleIds.First()
                : (allRoleIds.Count > 0 ? allRoleIds.First() : (Guid?)null));

        dto.RoleId = (activeRoleId.HasValue && activeRoleId.Value != Guid.Empty) ? activeRoleId : null;
        dto.RoleIds = allRoleIds;
        dto.PrimaryRoleIds = primaryRoleIds;
        dto.SecondaryRoleIds = secondaryRoleIds;
        dto.Roles = allRoleNames;
        dto.PrimaryRoles = primaryRoleNames;
        dto.SecondaryRoles = secondaryRoleNames;
        dto.EnableMultipleRoles = user.EnableMultipleRoles;
        dto.IsPrimary = user.IsPrimary || activeUserRoles.Any(ur => ur.IsPrimary);
        dto.IsSecondary = user.IsSecondary || activeUserRoles.Any(ur => ur.IsSecondary);
        dto.ActiveRoleId = user.RoleId;

        if (dto.RoleIds.Count == 0 && dto.RoleId.HasValue)
        {
            dto.RoleIds.Add(dto.RoleId.Value);
        }
        if (dto.PrimaryRoleIds.Count == 0 && dto.RoleIds.Count > 0)
        {
            dto.PrimaryRoleIds.Add(dto.RoleIds.First());
            dto.IsPrimary = true;
        }

        return dto;
    }

    // ── GET ALL ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            return users.Where(u => !u.IsDeleted).OrderByDescending(u => u.CreatedOn).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    // ── GET PROFILE ──────────────────────────────────────────────────────────
    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            return await EnrichUserDtoWithMemberProfileAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetProfileAsync));
            throw;
        }
    }

    // ── CREATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> CreateAsync(CreateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailToUse = request.Email.Trim().ToLowerInvariant();
            var isAccessEnabled = (request.EnableUserAccess == true || request.CreateMemberProfile == true) ||
                                  (request.CreateMemberProfile != false && request.EnableUserAccess != false && (!string.IsNullOrWhiteSpace(request.Username) || !string.IsNullOrWhiteSpace(request.Password)));

            // Check duplicate email
            var existingUser = await _userRepository.GetByEmailAsync(emailToUse, cancellationToken);
            if (existingUser != null && !existingUser.IsDeleted)
            {
                var ownerName = !string.IsNullOrWhiteSpace(existingUser.FullName) ? existingUser.FullName : existingUser.Username;
                throw new InvalidOperationException($"Email '{request.Email.Trim()}' already exists in the database (registered to '{ownerName}').");
            }

            // Check duplicate phone
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var existingPhoneUser = await _userRepository.GetByPhoneAsync(request.Phone.Trim(), cancellationToken);
                if (existingPhoneUser != null && !existingPhoneUser.IsDeleted)
                {
                    var ownerName = !string.IsNullOrWhiteSpace(existingPhoneUser.FullName) ? existingPhoneUser.FullName : existingPhoneUser.Username;
                    throw new InvalidOperationException($"Phone number '{request.Phone.Trim()}' already exists in the database (registered to '{ownerName}').");
                }
            }

            var username = isAccessEnabled
                ? (!string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : emailToUse.Split('@')[0])
                : (!string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : emailToUse.Split('@')[0]);

            if (!string.IsNullOrWhiteSpace(username))
            {
                var usernameExists = await _userRepository.GetByUsernameAsync(username, cancellationToken);
                if (usernameExists != null && !usernameExists.IsDeleted)
                {
                    if (string.IsNullOrWhiteSpace(request.Username) && !isAccessEnabled)
                    {
                        username = $"{username}_{new Random().Next(100, 999)}";
                    }
                    else if (string.IsNullOrWhiteSpace(request.Username))
                    {
                        username = $"{username}_{new Random().Next(100, 999)}";
                    }
                    else
                    {
                        var ownerName = !string.IsNullOrWhiteSpace(usernameExists.FullName) ? usernameExists.FullName : usernameExists.Username;
                        throw new InvalidOperationException($"Username '{username}' already exists in the database (registered to '{ownerName}').");
                    }
                }
            }

            var isMultipleRoles = request.EnableMultipleRoles ?? false;
            var primaryRolesReq = (request.PrimaryRoles ?? new List<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.RoleName) ? new[] { request.RoleName } : Enumerable.Empty<string>())
                .SelectMany(r => r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var secondaryRolesReq = (request.SecondaryRoles ?? new List<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.SecondaryRolesCsv) ? request.SecondaryRolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.SecondaryRole) ? request.SecondaryRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .Concat((request.Roles ?? new List<string>()))
                .Concat(!string.IsNullOrWhiteSpace(request.RolesCsv) ? request.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .SelectMany(r => r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Filter primary role out from secondary role entries so there's no conflict in DB flags
            secondaryRolesReq = secondaryRolesReq
                .Where(r => !primaryRolesReq.Contains(r, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (isMultipleRoles)
            {
                if (primaryRolesReq.Count == 0 && secondaryRolesReq.Count > 0)
                {
                    primaryRolesReq.Add(secondaryRolesReq.First());
                    secondaryRolesReq.RemoveAt(0);
                }
                else if (primaryRolesReq.Count == 0)
                {
                    primaryRolesReq.Add(CommonRoles.Member);
                }
            }

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var rolesDict = allRoles.ToDictionary(r => r.RoleName.Trim(), r => r.RoleId, StringComparer.OrdinalIgnoreCase);
            var roleNamesDict = allRoles.ToDictionary(r => r.RoleId, r => r.RoleName, EqualityComparer<Guid>.Default);
            var rolesToAdd = new List<AppUserRole>();
            Guid? assignedRoleId = null;
            string primaryRoleNameForUser = "Member";

            if (!isAccessEnabled)
            {
                primaryRoleNameForUser = string.Empty;
                assignedRoleId = null;
                rolesToAdd.Clear();
            }
            else if (isMultipleRoles)
            {
                primaryRoleNameForUser = primaryRolesReq.First();
                foreach (var pRoleName in primaryRolesReq)
                {
                    if (!rolesDict.TryGetValue(pRoleName, out var roleId))
                    {
                        var newRoleEntity = new Role
                        {
                            RoleId = Guid.NewGuid(),
                            RoleName = pRoleName,
                            DefaultContributionAmount = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedOn = DateTime.UtcNow
                        };
                        await _roleRepository.AddAsync(newRoleEntity, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        roleId = newRoleEntity.RoleId;
                        rolesDict[pRoleName] = roleId;
                        roleNamesDict[roleId] = pRoleName;
                    }
                    if (!assignedRoleId.HasValue) assignedRoleId = roleId;
                    rolesToAdd.Add(new AppUserRole
                    {
                        RoleId = roleId,
                        IsPrimary = true,
                        IsSecondary = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var sRoleName in secondaryRolesReq)
                {
                    if (!rolesDict.TryGetValue(sRoleName, out var roleId))
                    {
                        var newRoleEntity = new Role
                        {
                            RoleId = Guid.NewGuid(),
                            RoleName = sRoleName,
                            DefaultContributionAmount = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedOn = DateTime.UtcNow
                        };
                        await _roleRepository.AddAsync(newRoleEntity, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        roleId = newRoleEntity.RoleId;
                        rolesDict[sRoleName] = roleId;
                        roleNamesDict[roleId] = sRoleName;
                    }
                    rolesToAdd.Add(new AppUserRole
                    {
                        RoleId = roleId,
                        IsPrimary = false,
                        IsSecondary = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                var roleName = !string.IsNullOrWhiteSpace(request.RoleName) ? request.RoleName.Trim() : "Member";
                primaryRoleNameForUser = roleName;
                if (!rolesDict.TryGetValue(roleName, out var roleId))
                {
                    var newRoleEntity = new Role
                    {
                        RoleId = Guid.NewGuid(),
                        RoleName = roleName,
                        DefaultContributionAmount = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    };
                    await _roleRepository.AddAsync(newRoleEntity, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    roleId = newRoleEntity.RoleId;
                    rolesDict[roleName] = roleId;
                    roleNamesDict[roleId] = roleName;
                }
                assignedRoleId = roleId;
                rolesToAdd.Add(new AppUserRole
                {
                    RoleId = roleId,
                    IsPrimary = true,
                    IsSecondary = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!Enum.TryParse<UserRole>(primaryRoleNameForUser, ignoreCase: true, out var enumRole))
            {
                enumRole = UserRole.Member;
            }

            var fullName = !string.IsNullOrWhiteSpace(request.FullName)
                ? request.FullName.Trim()
                : (!string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : emailToUse.Split('@')[0]);

            var rawPassword = !string.IsNullOrWhiteSpace(request.Password)
                ? request.Password.Trim()
                : $"Pass@{new Random().Next(100000, 999999)}";

            var defaultWorkType = await ResolveDefaultWorkTypeAsync(cancellationToken);
            var assignedWorkTypeName = !string.IsNullOrWhiteSpace(request.WorkType) ? request.WorkType.Trim() : defaultWorkType;
            var assignedWorkTypeId = await ResolveWorkTypeIdAsync(assignedWorkTypeName, cancellationToken);

            var appUser = new AppUser
            {
                UserId              = Guid.NewGuid(),
                Username            = username,
                Email               = emailToUse,
                PasswordHash        = isAccessEnabled ? _passwordHasher.HashPassword(rawPassword) : string.Empty,
                Role                = enumRole,
                FullName            = fullName,
                IsActive            = request.IsActive,
                IsFirstLogin        = false,
                EnableMultipleRoles = isAccessEnabled && isMultipleRoles,
                IsPrimary           = isAccessEnabled,
                IsSecondary         = isAccessEnabled && isMultipleRoles && secondaryRolesReq.Count > 0,
                Phone               = request.Phone?.Trim() ?? string.Empty,
                Gender              = !string.IsNullOrWhiteSpace(request.Gender) ? request.Gender.Trim() : "Male",
                WorkType            = assignedWorkTypeName,
                WorkTypeId          = assignedWorkTypeId,
                RoleId              = isAccessEnabled ? assignedRoleId : null,
                DateOfBirth         = (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                    ? request.DateOfBirth.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                JoiningDate         = (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                    ? request.JoiningDate.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                CreatedBy           = CommonMethods.ParseNullableGuid(user),
                CreatedAt           = DateTime.UtcNow,
                CreatedOn           = DateTime.UtcNow
            };

            appUser.UserRoles.Clear();
            foreach (var ur in rolesToAdd.GroupBy(r => r.RoleId).Select(g => g.First()))
            {
                ur.UserId = appUser.UserId;
                appUser.UserRoles.Add(ur);
            }

            await _userRepository.AddAsync(appUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send credentials email
            if (isAccessEnabled)
            {
                try
                {
                    var subject = "Welcome to Team Contribution Management System - Your Account Details";
                    var emailBody = $@"
<div style=""font-family: 'Outfit', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f7f6fb; padding: 40px 20px; border-radius: 16px; max-width: 600px; margin: 0 auto; color: #1e1a2e; border: 1px solid rgba(74, 63, 107, 0.08);"">
    <div style=""text-align: center; margin-bottom: 25px;"">
        <h2 style=""margin: 0; color: #7c3aed; font-weight: 900; letter-spacing: 0.05em;"">TEAM CONTRIBUTION</h2>
        <span style=""font-size: 12px; color: #5b5280; font-weight: 700; text-transform: uppercase;"">Management System</span>
    </div>
    <div style=""background-color: #ffffff; border-radius: 12px; padding: 30px; box-shadow: 0 10px 30px rgba(30, 26, 46, 0.03);"">
        <h3 style=""margin-top: 0; color: #1e1a2e; font-weight: 800; font-size: 18px;"">Welcome to the Team!</h3>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">Hello <strong>{fullName}</strong>,</p>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">Your user account has been successfully created. Here are your login credentials:</p>
        
        <table style=""width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 14px;"">
            <tr style=""border-bottom: 1px solid #f1f0f7;"">
                <td style=""padding: 10px 0; color: #64748b; font-weight: 600; width: 40%;"">Username:</td>
                <td style=""padding: 10px 0; color: #1e1a2e; font-weight: 700;"">{appUser.Username}</td>
            </tr>
            <tr style=""border-bottom: 1px solid #f1f0f7;"">
                <td style=""padding: 10px 0; color: #64748b; font-weight: 600;"">Email:</td>
                <td style=""padding: 10px 0; color: #1e1a2e; font-weight: 700;"">{appUser.Email}</td>
            </tr>
            <tr style=""border-bottom: 1px solid #f1f0f7;"">
                <td style=""padding: 10px 0; color: #64748b; font-weight: 600;"">Temporary Password:</td>
                <td style=""padding: 10px 0; color: #7c3aed; font-family: monospace; font-weight: 700; font-size: 15px;"">{rawPassword}</td>
            </tr>
            <tr style=""border-bottom: 1px solid #f1f0f7;"">
                <td style=""padding: 10px 0; color: #64748b; font-weight: 600;"">Assigned Role:</td>
                <td style=""padding: 10px 0; color: #1e1a2e; font-weight: 700;"">{enumRole}</td>
            </tr>
        </table>

        <div style=""background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 14px 16px; margin: 20px 0;"">
            <p style=""margin: 0; color: #166534; font-size: 13px; line-height: 1.5; font-weight: 600;"">
                &#10003; You can use the above password to log in directly to the portal. If you want to change your password, you can change it anytime in your Profile.
            </p>
        </div>

        <p style=""color: #5b5280; font-size: 13px; line-height: 1.6; margin-bottom: 0;"">
            Please keep your credentials secure.
        </p>
    </div>
    <div style=""text-align: center; margin-top: 25px; color: #9d96bd; font-size: 12px;"">
        &copy; 2026 Team Contribution Management System. All rights reserved.
    </div>
</div>";

                    await _emailService.SendEmailAsync(appUser.Email, subject, emailBody, cancellationToken: cancellationToken);
                    _logger.LogInformation("Credentials email sent successfully to {Email}", appUser.Email);
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Failed to send welcome credentials email to {Email}", appUser.Email);
                }
            }

            var primaryRoleIdsList = rolesToAdd.Where(ur => ur.IsPrimary).Select(ur => ur.RoleId).Distinct().ToList();
            var secondaryRoleIdsList = rolesToAdd.Where(ur => ur.IsSecondary).Select(ur => ur.RoleId).Distinct().ToList();
            var allRoleIdsList = rolesToAdd.Select(ur => ur.RoleId).Distinct().ToList();

            var returnDto = new UserDto
            {
                UserId = appUser.UserId,
                Username = appUser.Username,
                FullName = appUser.FullName,
                Email = appUser.Email,
                Phone = appUser.Phone,
                Gender = appUser.Gender,
                WorkType = appUser.WorkType,
                DateOfBirth = appUser.DateOfBirth,
                JoiningDate = appUser.JoiningDate,
                RoleId = isAccessEnabled
                    ? ((appUser.RoleId.HasValue && appUser.RoleId.Value != Guid.Empty)
                        ? appUser.RoleId
                        : (primaryRoleIdsList.Count > 0 ? primaryRoleIdsList.First() : (Guid?)null))
                    : (Guid?)null,
                Role = isAccessEnabled ? primaryRoleNameForUser : null,
                RoleName = isAccessEnabled ? primaryRoleNameForUser : null,
                Roles = isAccessEnabled ? rolesToAdd.Select(r => roleNamesDict.TryGetValue(r.RoleId, out var n) ? n : r.RoleId.ToString()).Distinct().ToList() : new List<string>(),
                PrimaryRoles = isAccessEnabled ? rolesToAdd.Where(r => r.IsPrimary).Select(r => roleNamesDict.TryGetValue(r.RoleId, out var n) ? n : r.RoleId.ToString()).Distinct().ToList() : new List<string>(),
                SecondaryRoles = isAccessEnabled ? rolesToAdd.Where(r => r.IsSecondary).Select(r => roleNamesDict.TryGetValue(r.RoleId, out var n) ? n : r.RoleId.ToString()).Distinct().ToList() : new List<string>(),
                RoleIds = allRoleIdsList,
                PrimaryRoleIds = primaryRoleIdsList,
                SecondaryRoleIds = secondaryRoleIdsList,
                EnableMultipleRoles = appUser.EnableMultipleRoles,
                IsPrimary = appUser.IsPrimary,
                IsSecondary = appUser.IsSecondary,
                ActiveRoleId = appUser.RoleId,
                IsActive = appUser.IsActive,
                HasMemberProfile = true,
                IsFirstLogin = appUser.IsFirstLogin,
                CreatedOn = appUser.CreatedOn,
                CreatedAt = appUser.CreatedAt
            };

            _logger.LogInformation("User created successfully with Email {Email}", emailToUse);
            return returnDto;
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error in CreateAsync: {Message}", msg);
            throw;
        }
    }

    // ── UPDATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var newEmail = request.Email.Trim().ToLowerInvariant();

            // Find existing AppUser
            var appUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (appUser == null)
            {
                appUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            }

            if (appUser == null)
            {
                throw new KeyNotFoundException(CommonMessages.Users.NotFound);
            }

            // Check email uniqueness
            var emailUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (emailUser != null && emailUser.UserId != appUser.UserId && !emailUser.IsDeleted)
            {
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);
            }

            var isMultipleRoles = request.EnableMultipleRoles ?? appUser.EnableMultipleRoles;
            var primaryRolesReq = (request.PrimaryRoles ?? new List<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.RoleName) ? new[] { request.RoleName } : Enumerable.Empty<string>())
                .SelectMany(r => r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var secondaryRolesReq = (request.SecondaryRoles ?? new List<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.SecondaryRolesCsv) ? request.SecondaryRolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .Concat(!string.IsNullOrWhiteSpace(request.SecondaryRole) ? request.SecondaryRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .Concat((request.Roles ?? new List<string>()))
                .Concat(!string.IsNullOrWhiteSpace(request.RolesCsv) ? request.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Enumerable.Empty<string>())
                .SelectMany(r => r.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Filter primary role out from secondary role entries so there's no conflict in DB flags
            secondaryRolesReq = secondaryRolesReq
                .Where(r => !primaryRolesReq.Contains(r, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (isMultipleRoles && (request.PrimaryRoles != null || request.SecondaryRoles != null || !string.IsNullOrWhiteSpace(request.SecondaryRolesCsv) || !string.IsNullOrWhiteSpace(request.SecondaryRole)))
            {
                if (primaryRolesReq.Count == 0 && secondaryRolesReq.Count > 0)
                {
                    primaryRolesReq.Add(secondaryRolesReq.First());
                    secondaryRolesReq.RemoveAt(0);
                }
                else if (primaryRolesReq.Count == 0)
                {
                    primaryRolesReq.Add(CommonRoles.Member);
                }
            }

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var rolesDict = allRoles.ToDictionary(r => r.RoleName.Trim(), r => r.RoleId, StringComparer.OrdinalIgnoreCase);
            var roleNamesDict = allRoles.ToDictionary(r => r.RoleId, r => r.RoleName, EqualityComparer<Guid>.Default);
            var rolesToAdd = new List<AppUserRole>();
            string primaryRoleNameForUser = "Member";

            if (isMultipleRoles && (request.PrimaryRoles != null || request.SecondaryRoles != null))
            {
                primaryRoleNameForUser = primaryRolesReq.First();
                var desiredRoles = new List<(Guid RoleId, bool IsPrimary, bool IsSecondary)>();

                foreach (var pRoleName in primaryRolesReq)
                {
                    if (!rolesDict.TryGetValue(pRoleName, out var roleId))
                    {
                        var newRoleEntity = new Role
                        {
                            RoleId = Guid.NewGuid(),
                            RoleName = pRoleName,
                            DefaultContributionAmount = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedOn = DateTime.UtcNow
                        };
                        await _roleRepository.AddAsync(newRoleEntity, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        roleId = newRoleEntity.RoleId;
                        rolesDict[pRoleName] = roleId;
                        roleNamesDict[roleId] = pRoleName;
                    }
                    desiredRoles.Add((roleId, true, false));
                }

                foreach (var sRoleName in secondaryRolesReq)
                {
                    if (!rolesDict.TryGetValue(sRoleName, out var roleId))
                    {
                        var newRoleEntity = new Role
                        {
                            RoleId = Guid.NewGuid(),
                            RoleName = sRoleName,
                            DefaultContributionAmount = 0,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedOn = DateTime.UtcNow
                        };
                        await _roleRepository.AddAsync(newRoleEntity, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        roleId = newRoleEntity.RoleId;
                        rolesDict[sRoleName] = roleId;
                        roleNamesDict[roleId] = sRoleName;
                    }
                    desiredRoles.Add((roleId, false, true));
                }

                var desiredMap = desiredRoles
                    .GroupBy(d => d.RoleId)
                    .ToDictionary(g => g.Key, g => (IsPrimary: g.Any(x => x.IsPrimary), IsSecondary: g.All(x => !x.IsPrimary) && g.Any(x => x.IsSecondary)));

                var rolesToRemove = appUser.UserRoles.Where(ur => !desiredMap.ContainsKey(ur.RoleId)).ToList();
                foreach (var ur in rolesToRemove)
                {
                    appUser.UserRoles.Remove(ur);
                }

                foreach (var (rId, config) in desiredMap)
                {
                    var existing = appUser.UserRoles.FirstOrDefault(ur => ur.RoleId == rId);
                    if (existing != null)
                    {
                        existing.IsPrimary = config.IsPrimary;
                        existing.IsSecondary = config.IsSecondary;
                    }
                    else
                    {
                        appUser.UserRoles.Add(new AppUserRole
                        {
                            UserId = appUser.UserId,
                            RoleId = rId,
                            IsPrimary = config.IsPrimary,
                            IsSecondary = config.IsSecondary,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                appUser.EnableMultipleRoles = true;
                appUser.IsPrimary = true;
                appUser.IsSecondary = secondaryRolesReq.Count > 0;

                var primaryRoleId = desiredRoles.FirstOrDefault(d => d.IsPrimary).RoleId;
                var matchedActiveRole = !string.IsNullOrWhiteSpace(request.RoleName) && rolesDict.TryGetValue(request.RoleName.Trim(), out var requestedActiveRoleId) && desiredMap.ContainsKey(requestedActiveRoleId)
                    ? requestedActiveRoleId
                    : primaryRoleId;
                appUser.RoleId = matchedActiveRole != Guid.Empty ? matchedActiveRole : primaryRoleId;
            }
            else if (isMultipleRoles)
            {
                // Preserving existing multiple roles, allow updating active RoleId if RoleName is provided
                appUser.EnableMultipleRoles = true;
                if (!string.IsNullOrWhiteSpace(request.RoleName) && rolesDict.TryGetValue(request.RoleName.Trim(), out var activeRoleId))
                {
                    appUser.RoleId = activeRoleId;
                    primaryRoleNameForUser = request.RoleName.Trim();
                }
                else
                {
                    var existingPrimary = appUser.UserRoles.FirstOrDefault(ur => ur.IsPrimary);
                    if (existingPrimary != null && roleNamesDict.TryGetValue(existingPrimary.RoleId, out var pName))
                    {
                        primaryRoleNameForUser = pName;
                    }
                }
            }
            else
            {
                var roleName = !string.IsNullOrWhiteSpace(request.RoleName) ? request.RoleName.Trim() : appUser.Role.ToString();
                var matchedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase)) ?? allRoles.FirstOrDefault();
                primaryRoleNameForUser = matchedRole?.RoleName ?? roleName;
                if (matchedRole != null)
                {
                    var rolesToRemove = appUser.UserRoles.Where(ur => ur.RoleId != matchedRole.RoleId).ToList();
                    foreach (var ur in rolesToRemove)
                    {
                        appUser.UserRoles.Remove(ur);
                    }
                    var existing = appUser.UserRoles.FirstOrDefault(ur => ur.RoleId == matchedRole.RoleId);
                    if (existing != null)
                    {
                        existing.IsPrimary = true;
                        existing.IsSecondary = false;
                    }
                    else
                    {
                        appUser.UserRoles.Add(new AppUserRole
                        {
                            UserId = appUser.UserId,
                            RoleId = matchedRole.RoleId,
                            IsPrimary = true,
                            IsSecondary = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    appUser.RoleId = matchedRole.RoleId;
                }
                appUser.EnableMultipleRoles = false;
                appUser.IsPrimary = true;
                appUser.IsSecondary = false;
            }

            if (!Enum.TryParse<UserRole>(primaryRoleNameForUser, ignoreCase: true, out var enumRole))
            {
                enumRole = UserRole.Member;
            }

            var fullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : appUser.FullName;
            var username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : appUser.Username;

            var usernameOwner = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (usernameOwner != null && usernameOwner.UserId != appUser.UserId && !usernameOwner.IsDeleted)
            {
                throw new InvalidOperationException(CommonMessages.Users.UsernameExists);
            }

            var oldUsername = appUser.Username;
            appUser.Username = username;
            appUser.Email = newEmail;
            appUser.FullName = fullName;
            appUser.Role = enumRole;
            appUser.IsActive = request.IsActive;

            if (!string.IsNullOrWhiteSpace(request.Phone)) appUser.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Gender)) appUser.Gender = request.Gender.Trim();
            if (!string.IsNullOrWhiteSpace(request.WorkType))
            {
                appUser.WorkType = request.WorkType.Trim();
                appUser.WorkTypeId = await ResolveWorkTypeIdAsync(request.WorkType.Trim(), cancellationToken);
            }
            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default) appUser.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
            if (request.JoiningDate.HasValue && request.JoiningDate.Value != default) appUser.JoiningDate = request.JoiningDate.Value.ToUniversalTime();

            appUser.ModifiedBy = CommonMethods.ParseNullableGuid(user);
            appUser.ModifiedOn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                appUser.PasswordHash = _passwordHasher.HashPassword(request.Password.Trim());
                appUser.IsFirstLogin = false;
            }

            _userRepository.Update(appUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.Equals(oldUsername, username, StringComparison.OrdinalIgnoreCase))
            {
                await _userRepository.CascadeUpdateCreatorDisplayNameAsync(appUser.UserId, oldUsername, username, cancellationToken);
            }

            var primaryRoleIdsList = appUser.UserRoles.Where(ur => ur.IsPrimary).Select(ur => ur.RoleId).Distinct().ToList();
            var secondaryRoleIdsList = appUser.UserRoles.Where(ur => ur.IsSecondary).Select(ur => ur.RoleId).Distinct().ToList();
            var allRoleIdsList = appUser.UserRoles.Select(ur => ur.RoleId).Distinct().ToList();

            var returnDto = new UserDto
            {
                UserId = appUser.UserId,
                Username = appUser.Username,
                FullName = appUser.FullName,
                Email = appUser.Email,
                Phone = appUser.Phone,
                Gender = appUser.Gender,
                WorkType = appUser.WorkType,
                DateOfBirth = appUser.DateOfBirth,
                JoiningDate = appUser.JoiningDate,
                RoleId = (appUser.RoleId.HasValue && appUser.RoleId.Value != Guid.Empty)
                    ? appUser.RoleId
                    : (primaryRoleIdsList.Count > 0 ? primaryRoleIdsList.First() : (Guid?)null),
                RoleIds = allRoleIdsList,
                PrimaryRoleIds = primaryRoleIdsList,
                SecondaryRoleIds = secondaryRoleIdsList,
                EnableMultipleRoles = appUser.EnableMultipleRoles,
                IsPrimary = appUser.IsPrimary,
                IsSecondary = appUser.IsSecondary,
                ActiveRoleId = appUser.RoleId,
                IsActive = appUser.IsActive,
                HasMemberProfile = true,
                IsFirstLogin = appUser.IsFirstLogin,
                CreatedOn = appUser.CreatedOn,
                CreatedAt = appUser.CreatedAt
            };

            _logger.LogInformation("User updated successfully: {Id}", userId);
            return returnDto;
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error in UpdateAsync: {Message}", msg);
            throw;
        }
    }

    // ── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var appUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (appUser == null)
            {
                throw new KeyNotFoundException(CommonMessages.Users.NotFound);
            }

            appUser.IsDeleted = true;
            appUser.IsActive = false;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Users.UserDeleted, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }

    // ── UPDATE PROFILE ───────────────────────────────────────────────────────
    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            var oldEmail = user.Email;
            var newEmail = request.Email.Trim().ToLowerInvariant();

            var emailOwner = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (emailOwner is not null && emailOwner.UserId != userId)
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);

            var oldFullName = user.FullName;
            var newFullName = request.FullName.Trim();

            user.FullName = newFullName;
            user.Email    = newEmail;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Gender))
                user.Gender = request.Gender.Trim();
            if (!string.IsNullOrWhiteSpace(request.WorkType))
            {
                user.WorkType = request.WorkType.Trim();
                user.WorkTypeId = await ResolveWorkTypeIdAsync(request.WorkType.Trim(), cancellationToken);
            }
            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                user.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
            if (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                user.JoiningDate = request.JoiningDate.Value.ToUniversalTime();

            if (!string.IsNullOrWhiteSpace(request.RoleName))
            {
                var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
                var matchedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, request.RoleName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matchedRole != null)
                {
                    var existingRole = user.UserRoles.FirstOrDefault();
                    if (existingRole != null)
                    {
                        if (existingRole.RoleId != matchedRole.RoleId)
                        {
                            existingRole.RoleId = matchedRole.RoleId;
                        }
                    }
                    else
                    {
                        user.UserRoles.Add(new AppUserRole
                        {
                            UserId = user.UserId,
                            RoleId = matchedRole.RoleId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(request.Password);
                user.IsFirstLogin = false;
            }

            // Handle profile image upload (Direct Base64 Data URL stored in users.profile_image)
            if (string.IsNullOrWhiteSpace(request.ProfileImage))
            {
                user.ProfileImage = null;
            }
            else if (request.ProfileImage.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                ValidateBase64Image(request.ProfileImage);
                user.ProfileImage = request.ProfileImage;
            }
            else
            {
                user.ProfileImage = request.ProfileImage;
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(oldFullName) && !string.Equals(oldFullName, newFullName, StringComparison.OrdinalIgnoreCase))
            {
                await _userRepository.CascadeUpdateCreatorDisplayNameAsync(user.UserId, oldFullName, newFullName, cancellationToken);
            }

            var updated = await _userRepository.GetByIdAsync(user.UserId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            _logger.LogInformation(CommonLogMessages.Users.ProfileUpdated, user.UserId);
            return await EnrichUserDtoWithMemberProfileAsync(updated, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateProfileAsync));
            throw;
        }
    }

    private static void ValidateBase64Image(string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl)) return;

        var lower = dataUrl.ToLowerInvariant();
        bool isValidFormat = lower.StartsWith("data:image/jpeg;base64,") ||
                            lower.StartsWith("data:image/jpg;base64,") ||
                            lower.StartsWith("data:image/png;base64,") ||
                            lower.StartsWith("data:image/webp;base64,");

        if (!isValidFormat)
        {
            throw new InvalidOperationException("Invalid image format. Only JPG, JPEG, PNG, and WEBP image data URLs are allowed.");
        }

        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0 || commaIndex >= dataUrl.Length - 1)
        {
            throw new InvalidOperationException("Invalid Base64 image data URL format.");
        }

        var base64Part = dataUrl.Substring(commaIndex + 1).Trim();
        try
        {
            var bytes = Convert.FromBase64String(base64Part);
            if (bytes.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Profile image size exceeds the maximum allowed limit of 5MB.");
            }
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Invalid Base64 encoded image content.");
        }
    }
}
