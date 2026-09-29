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

    // ── HELPER: ENRICH USER DTO WITH MEMBER PROFILE ──────────────────────────
    private async Task<UserDto> EnrichUserDtoWithMemberProfileAsync(AppUser user, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<UserDto>(user);
        dto.IsFirstLogin = user.IsFirstLogin;
        dto.HasMemberProfile = true;
        var member = await _memberRepository.GetByEmailAsync(user.Email, cancellationToken);
        if (member != null)
        {
            user.Members = new List<Member> { member };
            dto.MemberUsername = member.Name;
            dto.DateOfBirth = member.DateOfBirth;
            dto.JoiningDate = member.JoiningDate;
            dto.Gender = member.Gender;
            dto.Phone = member.Phone;
            dto.WorkType = !string.IsNullOrWhiteSpace(member.WorkType)
                ? member.WorkType
                : await ResolveDefaultWorkTypeAsync(cancellationToken);
            if (member.Role != null && !string.IsNullOrWhiteSpace(member.Role.RoleName))
            {
                dto.RoleName = member.Role.RoleName;
            }
        }
        else
        {
            dto.WorkType = await ResolveDefaultWorkTypeAsync(cancellationToken);
        }
        return dto;
    }

    // ── GET ALL ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            var members = await _memberRepository.GetAllAsync(cancellationToken);

            var userDict = users
                .Where(u => !u.IsDeleted)
                .GroupBy(u => u.Email.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var resultList = new List<UserDto>();
            var processedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Process all non-deleted members
            foreach (var member in members.Where(m => !m.IsDeleted))
            {
                var emailKey = member.Email.Trim().ToLowerInvariant();
                processedEmails.Add(emailKey);

                var userDto = new UserDto
                {
                    FullName = member.Name,
                    Email = member.Email,
                    Phone = member.Phone,
                    Gender = member.Gender,
                    WorkType = member.WorkType,
                    DateOfBirth = member.DateOfBirth,
                    JoiningDate = member.JoiningDate,
                    RoleName = !string.IsNullOrWhiteSpace(member.RoleName) ? member.RoleName : "Member",
                    IsActive = member.IsActive,
                    CreatedAt = member.CreatedAt,
                    CreatedOn = member.CreatedOn ?? member.CreatedAt ?? DateTime.UtcNow,
                    CreatedBy = member.CreatedBy,
                };

                if (userDict.TryGetValue(emailKey, out var user))
                {
                    userDto.UserId = user.UserId;
                    userDto.Username = user.Username;
                    userDto.RoleName = !string.IsNullOrWhiteSpace(user.RoleName) ? user.RoleName : (!string.IsNullOrWhiteSpace(member.RoleName) ? member.RoleName : "Member");
                    userDto.IsActive = user.IsActive;
                    userDto.IsFirstLogin = user.IsFirstLogin;
                    userDto.HasMemberProfile = true; // Has login access
                    userDto.ProfileImage = user.ProfileImage;
                }
                else
                {
                    // Member without User login access
                    userDto.UserId = member.MemberId;
                    userDto.Username = string.Empty;
                    userDto.HasMemberProfile = false; // No login access
                }

                resultList.Add(userDto);
            }

            // 2. Process any remaining AppUsers who don't have a member record
            foreach (var user in users.Where(u => !u.IsDeleted))
            {
                var emailKey = user.Email.Trim().ToLowerInvariant();
                if (!processedEmails.Contains(emailKey))
                {
                    var uDto = _mapper.Map<UserDto>(user);
                    uDto.HasMemberProfile = true;
                    resultList.Add(uDto);
                }
            }

            return resultList.OrderByDescending(u => u.CreatedOn).ToList();
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

            // Check duplicate email in members or users
            var existingMember = await _memberRepository.GetByEmailAsync(emailToUse, cancellationToken);
            var existingUser = await _userRepository.GetByEmailAsync(emailToUse, cancellationToken);
            if ((existingMember != null && !existingMember.IsDeleted) || (existingUser != null && !existingUser.IsDeleted))
            {
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);
            }

            // Check duplicate username if user access enabled
            if (isAccessEnabled && !string.IsNullOrWhiteSpace(request.Username))
            {
                var usernameExists = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
                if (usernameExists != null && !usernameExists.IsDeleted)
                {
                    throw new InvalidOperationException(CommonMessages.Users.UsernameExists);
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

            // 1. Always create the Member record
            var member = new Member
            {
                MemberId = Guid.NewGuid(),
                Name = fullName,
                Email = emailToUse,
                Phone = request.Phone?.Trim() ?? string.Empty,
                Gender = request.Gender?.Trim() ?? "Male",
                WorkType = !string.IsNullOrWhiteSpace(request.WorkType) ? request.WorkType.Trim() : "Office",
                RoleId = assignedRoleId,
                DateOfBirth = (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                    ? request.DateOfBirth.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                JoiningDate = (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                    ? request.JoiningDate.Value.ToUniversalTime()
                    : DateTime.UtcNow.Date,
                IsActive = request.IsActive,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };
            await _memberRepository.AddAsync(member, cancellationToken);

            AppUser? appUser = null;
            string? rawPassword = null;

            // 2. If User Access is enabled -> Create AppUser login account
            if (isAccessEnabled)
            {
                rawPassword = !string.IsNullOrWhiteSpace(request.Password)
                    ? request.Password.Trim()
                    : $"Pass@{new Random().Next(100000, 999999)}";

                var username = !string.IsNullOrWhiteSpace(request.Username)
                    ? request.Username.Trim()
                    : emailToUse.Split('@')[0];

                appUser = new AppUser
                {
                    UserId       = Guid.NewGuid(),
                    Username     = username,
                    Email        = emailToUse,
                    PasswordHash = _passwordHasher.HashPassword(rawPassword),
                    Role         = enumRole,
                    FullName     = fullName,
                    IsActive     = request.IsActive,
                    IsFirstLogin = false,
                    CreatedBy    = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                    CreatedAt    = DateTime.UtcNow,
                    CreatedOn    = DateTime.UtcNow
                };
                await _userRepository.AddAsync(appUser, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 3. If User Access enabled, send credentials email
            if (isAccessEnabled && appUser != null && !string.IsNullOrWhiteSpace(rawPassword))
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
                UserId = appUser?.UserId ?? member.MemberId,
                Username = appUser?.Username ?? string.Empty,
                FullName = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                Gender = member.Gender,
                WorkType = member.WorkType,
                DateOfBirth = member.DateOfBirth,
                JoiningDate = member.JoiningDate,
                RoleName = matchedRole?.RoleName ?? roleName,
                IsActive = member.IsActive,
                HasMemberProfile = isAccessEnabled && appUser != null,
                IsFirstLogin = appUser?.IsFirstLogin ?? false,
                CreatedOn = member.CreatedOn ?? member.CreatedAt ?? DateTime.UtcNow,
                CreatedAt = member.CreatedAt
            };

            _logger.LogInformation("User/Member created successfully with Email {Email}", emailToUse);
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
            var isAccessEnabled = request.EnableUserAccess ?? request.CreateMemberProfile ?? (!string.IsNullOrWhiteSpace(request.Username) || !string.IsNullOrWhiteSpace(request.Password));
            var newEmail = request.Email.Trim().ToLowerInvariant();

            // 1. Find existing AppUser (by id or by email)
            var appUser = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (appUser == null)
            {
                appUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            }

            // 2. Find existing Member (by id or by email)
            var member = await _memberRepository.GetByIdAsync(userId, cancellationToken);
            if (member == null && appUser != null)
            {
                member = await _memberRepository.GetByEmailAsync(appUser.Email, cancellationToken);
            }
            if (member == null && !string.IsNullOrWhiteSpace(newEmail))
            {
                member = await _memberRepository.GetByEmailAsync(newEmail, cancellationToken);
            }

            if (appUser == null && member == null)
            {
                throw new KeyNotFoundException(CommonMessages.Users.NotFound);
            }

            // Check email uniqueness
            var emailUser = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (emailUser != null && emailUser.UserId != appUser?.UserId && emailUser.UserId != userId && !emailUser.IsDeleted)
            {
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);
            }
            var emailMember = await _memberRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (emailMember != null && emailMember.MemberId != member?.MemberId && emailMember.MemberId != userId && !emailMember.IsDeleted)
            {
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);
            }

            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
            var roleName = !string.IsNullOrWhiteSpace(request.RoleName) ? request.RoleName.Trim() : (appUser?.Role.ToString() ?? "Member");
            var matchedRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase)) ?? allRoles.FirstOrDefault();
            if (!Enum.TryParse<UserRole>(roleName, ignoreCase: true, out var enumRole))
            {
                enumRole = UserRole.Member;
            }

            var fullName = !string.IsNullOrWhiteSpace(request.FullName) ? request.FullName.Trim() : (member?.Name ?? appUser?.FullName ?? string.Empty);

            // 3. Update or Create Member
            if (member != null)
            {
                member.Name = fullName;
                member.Email = newEmail;
                if (!string.IsNullOrWhiteSpace(request.Phone)) member.Phone = request.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(request.Gender)) member.Gender = request.Gender.Trim();
                if (!string.IsNullOrWhiteSpace(request.WorkType)) member.WorkType = request.WorkType.Trim();
                if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default) member.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
                if (request.JoiningDate.HasValue && request.JoiningDate.Value != default) member.JoiningDate = request.JoiningDate.Value.ToUniversalTime();
                member.IsActive = request.IsActive;
                if (matchedRole != null) member.RoleId = matchedRole.RoleId;
                if (!string.IsNullOrWhiteSpace(user)) member.ModifiedBy = user.Trim();
                member.ModifiedOn = DateTime.UtcNow;

                _memberRepository.Update(member);
            }
            else
            {
                member = new Member
                {
                    MemberId    = Guid.NewGuid(),
                    Name        = fullName,
                    Email       = newEmail,
                    Phone       = request.Phone?.Trim() ?? string.Empty,
                    Gender      = request.Gender?.Trim() ?? "Male",
                    WorkType    = !string.IsNullOrWhiteSpace(request.WorkType) ? request.WorkType.Trim() : "Office",
                    RoleId      = matchedRole?.RoleId,
                    DateOfBirth = (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                        ? request.DateOfBirth.Value.ToUniversalTime()
                        : DateTime.UtcNow.Date,
                    JoiningDate = (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                        ? request.JoiningDate.Value.ToUniversalTime()
                        : DateTime.UtcNow.Date,
                    IsActive    = request.IsActive,
                    CreatedBy   = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                    CreatedAt   = DateTime.UtcNow,
                    CreatedOn   = DateTime.UtcNow
                };
                await _memberRepository.AddAsync(member, cancellationToken);
            }

            // 4. Update or Create AppUser (if isAccessEnabled) or Deactivate AppUser (if not enabled)
            if (isAccessEnabled)
            {
                var username = !string.IsNullOrWhiteSpace(request.Username) ? request.Username.Trim() : (appUser?.Username ?? newEmail.Split('@')[0]);

                // Check username uniqueness
                var usernameOwner = await _userRepository.GetByUsernameAsync(username, cancellationToken);
                if (usernameOwner != null && usernameOwner.UserId != appUser?.UserId && !usernameOwner.IsDeleted)
                {
                    throw new InvalidOperationException(CommonMessages.Users.UsernameExists);
                }

                if (appUser != null)
                {
                    var oldUsername = appUser.Username;
                    appUser.Username = username;
                    appUser.Email = newEmail;
                    appUser.FullName = fullName;
                    appUser.Role = enumRole;
                    appUser.IsActive = request.IsActive;
                    appUser.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
                    appUser.ModifiedOn = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(request.Password))
                    {
                        appUser.PasswordHash = _passwordHasher.HashPassword(request.Password.Trim());
                        appUser.IsFirstLogin = false;
                    }

                    _userRepository.Update(appUser);

                    if (!string.Equals(oldUsername, username, StringComparison.OrdinalIgnoreCase))
                    {
                        await _userRepository.CascadeUpdateCreatorDisplayNameAsync(appUser.UserId, oldUsername, username, cancellationToken);
                    }
                }
                else
                {
                    // User access newly enabled for this member!
                    var rawPassword = !string.IsNullOrWhiteSpace(request.Password) ? request.Password.Trim() : $"Pass@{new Random().Next(100000, 999999)}";
                    appUser = new AppUser
                    {
                        UserId = Guid.NewGuid(),
                        Username = username,
                        Email = newEmail,
                        PasswordHash = _passwordHasher.HashPassword(rawPassword),
                        Role = enumRole,
                        FullName = fullName,
                        IsActive = request.IsActive,
                        IsFirstLogin = false,
                        CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedOn = DateTime.UtcNow
                    };
                    await _userRepository.AddAsync(appUser, cancellationToken);

                    // Send email with credentials
                    try
                    {
                        var subject = "Your Team Contribution Management System Account Access";
                        var emailBody = $@"
<div style=""font-family: 'Outfit', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f7f6fb; padding: 40px 20px; border-radius: 16px; max-width: 600px; margin: 0 auto; color: #1e1a2e; border: 1px solid rgba(74, 63, 107, 0.08);"">
    <div style=""text-align: center; margin-bottom: 25px;"">
        <h2 style=""margin: 0; color: #7c3aed; font-weight: 900; letter-spacing: 0.05em;"">TEAM CONTRIBUTION</h2>
        <span style=""font-size: 12px; color: #5b5280; font-weight: 700; text-transform: uppercase;"">Management System</span>
    </div>
    <div style=""background-color: #ffffff; border-radius: 12px; padding: 30px; box-shadow: 0 10px 30px rgba(30, 26, 46, 0.03);"">
        <h3 style=""margin-top: 0; color: #1e1a2e; font-weight: 800; font-size: 18px;"">User Access Enabled</h3>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">Hello <strong>{fullName}</strong>,</p>
        <p style=""color: #5b5280; font-size: 14px; line-height: 1.6;"">Your user account access has been enabled. Here are your login credentials:</p>
        
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
    </div>
</div>";
                        await _emailService.SendEmailAsync(appUser.Email, subject, emailBody, cancellationToken: cancellationToken);
                    }
                    catch (Exception mailEx)
                    {
                        _logger.LogError(mailEx, "Failed to send credentials email upon enabling user access to {Email}", appUser.Email);
                    }
                }
            }
            else
            {
                // If user access is disabled, deactivate the AppUser if it existed
                if (appUser != null)
                {
                    appUser.IsActive = false;
                    _userRepository.Update(appUser);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var returnDto = new UserDto
            {
                UserId = appUser?.UserId ?? member.MemberId,
                Username = appUser?.Username ?? string.Empty,
                FullName = member.Name,
                Email = member.Email,
                Phone = member.Phone,
                Gender = member.Gender,
                WorkType = member.WorkType,
                DateOfBirth = member.DateOfBirth,
                JoiningDate = member.JoiningDate,
                RoleName = matchedRole?.RoleName ?? roleName,
                IsActive = member.IsActive,
                HasMemberProfile = isAccessEnabled && appUser != null && appUser.IsActive,
                IsFirstLogin = appUser?.IsFirstLogin ?? false,
                CreatedOn = member.CreatedOn ?? member.CreatedAt ?? DateTime.UtcNow,
                CreatedAt = member.CreatedAt
            };

            _logger.LogInformation("User/Member updated successfully: {Id}", userId);
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
            var member = await _memberRepository.GetByIdAsync(userId, cancellationToken);

            if (appUser != null && member == null)
            {
                member = await _memberRepository.GetByEmailAsync(appUser.Email, cancellationToken);
            }
            if (member != null && appUser == null)
            {
                appUser = await _userRepository.GetByEmailAsync(member.Email, cancellationToken);
            }

            if (member == null && appUser == null)
            {
                throw new KeyNotFoundException(CommonMessages.Users.NotFound);
            }

            if (member != null)
            {
                member.IsDeleted = true;
                member.IsActive = false;
                _memberRepository.Update(member);
            }

            if (appUser != null)
            {
                appUser.IsDeleted = true;
                appUser.IsActive = false;
                _userRepository.Delete(appUser);
            }

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
                    var relativePath = user.ProfileImage.Split('?')[0];
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, relativePath.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        try { File.Delete(oldFilePath); } catch {}
                    }
                }
                user.ProfileImage = null;
            }
            else if (request.ProfileImage.StartsWith(CommonConstants.Defaults.DataImagePrefix))
            {
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    var relativePath = user.ProfileImage.Split('?')[0];
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, relativePath.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        try { File.Delete(oldFilePath); } catch {}
                    }
                }

                var base64Data = request.ProfileImage.Substring(request.ProfileImage.IndexOf(CommonConstants.Defaults.Comma) + 1);
                var imageBytes = Convert.FromBase64String(base64Data);

                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), CommonConstants.Defaults.WwwRoot, CommonConstants.Defaults.UserImagesFolder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var extension = CommonConstants.Defaults.ExtPng;
                if (request.ProfileImage.Contains(CommonConstants.Defaults.ImageJpeg) || request.ProfileImage.Contains(CommonConstants.Defaults.ImageJpg))
                {
                    extension = CommonConstants.Defaults.ExtJpg;
                }

                var dateStr = DateTime.UtcNow.ToString(CommonConstants.Defaults.DateFormatYmd);
                var cleanUsername = user.Username.Replace(CommonConstants.Defaults.Space, CommonConstants.Defaults.Underscore).ToLowerInvariant();
                var fileName = $"{dateStr}{cleanUsername}.{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
                
                var cacheBuster = DateTime.UtcNow.Ticks;
                user.ProfileImage = $"{CommonConstants.Defaults.UserImagesPathPrefix}{fileName}{CommonConstants.Defaults.VersionParamPrefix}{cacheBuster}";
            }

            _userRepository.Update(user);

            // ── JOIN & UPDATE CORRESPONDING MEMBER RECORD ─────────────────────────
            var member = await _memberRepository.GetByEmailAsync(oldEmail, cancellationToken);
            if (member == null && !string.Equals(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                member = await _memberRepository.GetByEmailAsync(newEmail, cancellationToken);
            }

            var resolvedWorkType = !string.IsNullOrWhiteSpace(request.WorkType)
                ? request.WorkType.Trim()
                : (string.IsNullOrWhiteSpace(member?.WorkType)
                    ? await ResolveDefaultWorkTypeAsync(cancellationToken)
                    : member.WorkType);

            if (member != null)
            {
                member.Name = request.FullName.Trim();
                member.Email = newEmail;
                if (!string.IsNullOrWhiteSpace(request.Phone))
                    member.Phone = request.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(request.Gender))
                    member.Gender = request.Gender.Trim();
                member.WorkType = resolvedWorkType;
                if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                    member.DateOfBirth = request.DateOfBirth.Value.ToUniversalTime();
                if (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                    member.JoiningDate = request.JoiningDate.Value.ToUniversalTime();

                _memberRepository.Update(member);
            }
            else
            {
                var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
                var userRoleName = user.Role.ToString();
                var defaultRole = allRoles.FirstOrDefault(r => string.Equals(r.RoleName, userRoleName, StringComparison.OrdinalIgnoreCase)) 
                                  ?? allRoles.FirstOrDefault();

                if (defaultRole != null)
                {
                    var newMember = new Member
                    {
                        MemberId = Guid.NewGuid(),
                        Name = request.FullName.Trim(),
                        Email = newEmail,
                        Phone = request.Phone?.Trim() ?? string.Empty,
                        Gender = request.Gender?.Trim() ?? string.Empty,
                        WorkType = resolvedWorkType,
                        RoleId = defaultRole.RoleId,
                        DateOfBirth = (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default)
                            ? request.DateOfBirth.Value.ToUniversalTime()
                            : DateTime.UtcNow.Date,
                        JoiningDate = (request.JoiningDate.HasValue && request.JoiningDate.Value != default)
                            ? request.JoiningDate.Value.ToUniversalTime()
                            : DateTime.UtcNow.Date,
                        IsActive = user.IsActive
                    };
                    await _memberRepository.AddAsync(newMember, cancellationToken);
                }
            }

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
