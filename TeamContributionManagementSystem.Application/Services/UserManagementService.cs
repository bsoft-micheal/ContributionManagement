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

    public UserManagementService(ILogger<UserManagementService> logger, 
        IUserRepository userRepository,
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _logger = logger;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    // ── HELPER: ENRICH USER DTO WITH MEMBER PROFILE ──────────────────────────
    private async Task<UserDto> EnrichUserDtoWithMemberProfileAsync(AppUser user, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<UserDto>(user);
        var member = await _memberRepository.GetByEmailAsync(user.Email, cancellationToken);
        if (member != null)
        {
            user.Members = new List<Member> { member };
            dto.DateOfBirth = member.DateOfBirth;
            dto.JoiningDate = member.JoiningDate;
            dto.Gender = member.Gender;
            dto.Phone = member.Phone;
            dto.MemberType = member.MemberType;
            if (member.Role != null && !string.IsNullOrWhiteSpace(member.Role.RoleName))
            {
                dto.RoleName = member.Role.RoleName;
            }
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
            var memberDict = members.ToDictionary(m => m.Email.Trim().ToLowerInvariant(), m => m);

            foreach (var user in users)
            {
                if (memberDict.TryGetValue(user.Email.Trim().ToLowerInvariant(), out var member))
                {
                    user.DateOfBirth = member.DateOfBirth;
                    user.JoiningDate = member.JoiningDate;
                    user.Gender = member.Gender;
                    user.Phone = member.Phone;
                    user.MemberType = member.MemberType;
                    if (!string.IsNullOrWhiteSpace(member.RoleName))
                    {
                        user.RoleName = member.RoleName;
                    }
                }
            }
            return users;
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
            var emailExists = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (emailExists is not null)
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);

            var usernameExists = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (usernameExists is not null)
                throw new InvalidOperationException(CommonMessages.Users.UsernameExists);

            if (!Enum.TryParse<UserRole>(request.RoleName, ignoreCase: true, out var role))
                throw new InvalidOperationException(string.Format(CommonMessages.Roles.InvalidRoleFormat, request.RoleName));

            var appUser = new AppUser
            {
                UserId       = Guid.NewGuid(),
                Username     = request.Username.Trim(),
                Email        = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role         = role,
                FullName     = request.Username.Trim(),
                IsActive     = request.IsActive,
                CreatedBy    = string.IsNullOrWhiteSpace(user) ? null : user.Trim(),
                CreatedAt    = DateTime.UtcNow,
                CreatedOn    = DateTime.UtcNow
            };

            await _userRepository.AddAsync(appUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _userRepository.GetByIdAsync(appUser.UserId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            _logger.LogInformation(CommonLogMessages.Users.UserCreated, appUser.Username, appUser.UserId);
            return await EnrichUserDtoWithMemberProfileAsync(created, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    // ── UPDATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var appUser = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            var emailOwner = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (emailOwner is not null && emailOwner.UserId != userId)
                throw new InvalidOperationException(CommonMessages.Users.EmailExists);

            var usernameOwner = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (usernameOwner is not null && usernameOwner.UserId != userId)
                throw new InvalidOperationException(CommonMessages.Users.UsernameExists);

            if (!Enum.TryParse<UserRole>(request.RoleName, ignoreCase: true, out var role))
                throw new InvalidOperationException(string.Format(CommonMessages.Roles.InvalidRoleFormat, request.RoleName));

            var oldEmail = appUser.Email;
            var newEmail = request.Email.Trim().ToLowerInvariant();

            appUser.Username = request.Username.Trim();
            appUser.Email    = newEmail;
            appUser.Role     = role;
            appUser.IsActive = request.IsActive;
            appUser.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
            appUser.ModifiedOn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
                appUser.PasswordHash = _passwordHasher.HashPassword(request.Password);

            _userRepository.Update(appUser);

            if (!string.Equals(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var linkedMember = await _memberRepository.GetByEmailAsync(oldEmail, cancellationToken);
                if (linkedMember != null)
                {
                    linkedMember.Email = newEmail;
                    _memberRepository.Update(linkedMember);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updated = await _userRepository.GetByIdAsync(appUser.UserId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            _logger.LogInformation(CommonLogMessages.Users.UserUpdated, appUser.UserId);
            return await EnrichUserDtoWithMemberProfileAsync(updated, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    // ── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Users.NotFound);

            _userRepository.Delete(user);
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

            user.FullName = request.FullName.Trim();
            user.Email    = newEmail;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(request.Password);
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

            if (member != null)
            {
                member.Name = request.FullName.Trim();
                member.Email = newEmail;
                if (!string.IsNullOrWhiteSpace(request.Phone))
                    member.Phone = request.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(request.Gender))
                    member.Gender = request.Gender.Trim();
                if (!string.IsNullOrWhiteSpace(request.MemberType))
                    member.MemberType = request.MemberType.Trim();
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
                        MemberType = request.MemberType?.Trim() ?? string.Empty,
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
