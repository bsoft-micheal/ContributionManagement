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

        var roleFromMapping = user.UserRoles?.Select(ur => ur.Role?.RoleName).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        if (!string.IsNullOrWhiteSpace(roleFromMapping))
        {
            dto.RoleName = roleFromMapping;
        }
        else if (user.RoleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(user.RoleId.Value, cancellationToken);
            if (role != null)
            {
                dto.RoleName = role.RoleName;
            }
        }
        else if (string.IsNullOrWhiteSpace(dto.RoleName))
        {
            dto.RoleName = user.Role.ToString();
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
            var isAccessEnabled = request.EnableUserAccess ?? request.CreateMemberProfile ?? (!string.IsNullOrWhiteSpace(request.Username) || !string.IsNullOrWhiteSpace(request.Password));

            // Check duplicate email
            var existingUser = await _userRepository.GetByEmailAsync(emailToUse, cancellationToken);
            if (existingUser != null && !existingUser.IsDeleted)
            {
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);
            }

            var username = !string.IsNullOrWhiteSpace(request.Username)
                ? request.Username.Trim()
                : emailToUse.Split('@')[0];

            if (!string.IsNullOrWhiteSpace(username))
            {
                var usernameExists = await _userRepository.GetByUsernameAsync(username, cancellationToken);
                if (usernameExists != null && !usernameExists.IsDeleted)
                {
                    if (string.IsNullOrWhiteSpace(request.Username))
                    {
                        username = $"{username}_{new Random().Next(100, 999)}";
                    }
                    else
                    {
                        throw new InvalidOperationException(CommonMessages.Users.UsernameExists);
                    }
                }
            }

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var roleName = !string.IsNullOrWhiteSpace(request.RoleName) ? request.RoleName.Trim() : "Member";
            var matchedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase))
                              ?? allRoles.FirstOrDefault(r => string.Equals(r.RoleName, "Member", StringComparison.OrdinalIgnoreCase))
                              ?? allRoles.FirstOrDefault();

            Guid? assignedRoleId = matchedRole?.RoleId;
            if (assignedRoleId == null)
            {
                var newRole = new Role
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = "Member",
                    DefaultContributionAmount = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow
                };
                await _roleRepository.AddAsync(newRole, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                assignedRoleId = newRole.RoleId;
            }

            if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var enumRole))
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
                UserId       = Guid.NewGuid(),
                Username     = username,
                Email        = emailToUse,
                PasswordHash = _passwordHasher.HashPassword(rawPassword),
                Role         = enumRole,
                FullName     = fullName,
                IsActive     = request.IsActive,
                IsFirstLogin = false,
                Phone        = request.Phone?.Trim() ?? string.Empty,
                Gender       = !string.IsNullOrWhiteSpace(request.Gender) ? request.Gender.Trim() : "Male",
                WorkType     = assignedWorkTypeName,
                WorkTypeId   = assignedWorkTypeId,
                RoleId       = assignedRoleId,
                DateOfBirth  = (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                    ? request.DateOfBirth.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                JoiningDate  = (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                    ? request.JoiningDate.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                CreatedBy    = CommonMethods.ParseNullableGuid(user),
                CreatedAt    = DateTime.UtcNow,
                CreatedOn    = DateTime.UtcNow
            };

            if (assignedRoleId.HasValue && !appUser.UserRoles.Any(ur => ur.RoleId == assignedRoleId.Value))
            {
                appUser.UserRoles.Add(new AppUserRole
                {
                    UserId = appUser.UserId,
                    RoleId = assignedRoleId.Value,
                    CreatedAt = DateTime.UtcNow
                });
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
                RoleName = matchedRole?.RoleName ?? roleName,
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

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var roleName = !string.IsNullOrWhiteSpace(request.RoleName) ? request.RoleName.Trim() : appUser.Role.ToString();
            var matchedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase)) ?? allRoles.FirstOrDefault();
            if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var enumRole))
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
            if (matchedRole != null)
            {
                var existingRole = appUser.UserRoles.FirstOrDefault();
                if (existingRole != null)
                {
                    if (existingRole.RoleId != matchedRole.RoleId)
                    {
                        existingRole.RoleId = matchedRole.RoleId;
                    }
                }
                else
                {
                    appUser.UserRoles.Add(new AppUserRole
                    {
                        UserId = appUser.UserId,
                        RoleId = matchedRole.RoleId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
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
                RoleName = matchedRole?.RoleName ?? roleName,
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
            _userRepository.Delete(appUser);

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

            // Handle profile image upload
            if (request.ProfileImage == null)
            {
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    var relativePath = user.ProfileImage.Split('?')[0].TrimStart('/');
                    var deleteDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot),
                        Path.Combine(AppContext.BaseDirectory, CommonConstants.Defaults.WwwRoot)
                    };
                    foreach (var root in deleteDirs)
                    {
                        var oldFilePath = Path.Combine(root, relativePath);
                        if (File.Exists(oldFilePath))
                        {
                            try { File.Delete(oldFilePath); } catch {}
                        }
                    }
                }
                user.ProfileImage = null;
            }
            else if (request.ProfileImage.StartsWith(CommonConstants.Defaults.DataImagePrefix))
            {
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    var relativePath = user.ProfileImage.Split('?')[0].TrimStart('/');
                    var deleteDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot),
                        Path.Combine(AppContext.BaseDirectory, CommonConstants.Defaults.WwwRoot)
                    };
                    foreach (var root in deleteDirs)
                    {
                        var oldFilePath = Path.Combine(root, relativePath);
                        if (File.Exists(oldFilePath))
                        {
                            try { File.Delete(oldFilePath); } catch {}
                        }
                    }
                }

                var base64Data = request.ProfileImage.Substring(request.ProfileImage.IndexOf(CommonConstants.Defaults.Comma) + 1);
                var imageBytes = Convert.FromBase64String(base64Data);

                var extension = CommonConstants.Defaults.ExtPng;
                if (request.ProfileImage.Contains(CommonConstants.Defaults.ImageJpeg) || request.ProfileImage.Contains(CommonConstants.Defaults.ImageJpg))
                {
                    extension = CommonConstants.Defaults.ExtJpg;
                }

                var dateStr = DateTime.UtcNow.ToString(CommonConstants.Defaults.DateFormatYmd);
                var cleanUsername = user.Username.Replace(CommonConstants.Defaults.Space, CommonConstants.Defaults.Underscore).ToLowerInvariant();
                var fileName = $"{dateStr}{cleanUsername}.{extension}";

                var targetDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.UserImagesFolder),
                    Path.Combine(AppContext.BaseDirectory, CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.UserImagesFolder)
                };

                foreach (var folderPath in targetDirs)
                {
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    var filePath = Path.Combine(folderPath, fileName);
                    await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
                }
                
                var cacheBuster = DateTime.UtcNow.Ticks;
                user.ProfileImage = $"{CommonConstants.Defaults.UserImagesPathPrefix}{fileName}{CommonConstants.Defaults.VersionParamPrefix}{cacheBuster}";
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
}
