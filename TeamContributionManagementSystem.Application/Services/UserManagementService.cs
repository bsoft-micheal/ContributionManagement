using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly Microsoft.Extensions.Logging.ILogger<UserManagementService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public UserManagementService(Microsoft.Extensions.Logging.ILogger<UserManagementService> logger, 
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

            var dtos = new List<UserDto>();
            foreach (var user in users)
            {
                var dto = _mapper.Map<UserDto>(user);
                if (memberDict.TryGetValue(user.Email.Trim().ToLowerInvariant(), out var member))
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
                dtos.Add(dto);
            }
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
            throw;
        }
    }

    // ── GET PROFILE ──────────────────────────────────────────────────────────
    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            return await EnrichUserDtoWithMemberProfileAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetProfileAsync");
            throw;
        }
    }

    // ── CREATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Unique email check
            var emailExists = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (emailExists is not null)
                throw new InvalidOperationException("A user with this email already exists.");

            // Unique username check
            var usernameExists = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (usernameExists is not null)
                throw new InvalidOperationException("A user with this username already exists.");

            if (!Enum.TryParse<UserRole>(request.RoleName, ignoreCase: true, out var role))
                throw new InvalidOperationException($"Invalid role: '{request.RoleName}'. Valid values: Admin, Manager, User.");

            var user = new AppUser
            {
                UserId       = Guid.NewGuid(),
                Username     = request.Username.Trim(),
                Email        = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                Role         = role,
                FullName     = request.Username.Trim(), // default FullName to username; can be changed later
                IsActive     = request.IsActive,
                CreatedOn    = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _userRepository.GetByIdAsync(user.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("Created user could not be loaded.");

            return await EnrichUserDtoWithMemberProfileAsync(created, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    // ── UPDATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            // Email uniqueness (excluding self)
            var emailOwner = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (emailOwner is not null && emailOwner.UserId != userId)
                throw new InvalidOperationException("A user with this email already exists.");

            // Username uniqueness (excluding self)
            var usernameOwner = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (usernameOwner is not null && usernameOwner.UserId != userId)
                throw new InvalidOperationException("A user with this username already exists.");

            if (!Enum.TryParse<UserRole>(request.RoleName, ignoreCase: true, out var role))
                throw new InvalidOperationException($"Invalid role: '{request.RoleName}'. Valid values: Admin, Manager, User.");

            var oldEmail = user.Email;
            var newEmail = request.Email.Trim().ToLowerInvariant();

            user.Username = request.Username.Trim();
            user.Email    = newEmail;
            user.Role     = role;
            user.IsActive = request.IsActive;

            // Update password only when a new one is supplied
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = _passwordHasher.HashPassword(request.Password);

            _userRepository.Update(user);

            // Also keep linked Member email in sync if it changed
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

            var updated = await _userRepository.GetByIdAsync(user.UserId, cancellationToken)
                ?? throw new KeyNotFoundException("Updated user could not be loaded.");

            return await EnrichUserDtoWithMemberProfileAsync(updated, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    // ── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            _userRepository.Delete(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }

    // ── UPDATE PROFILE ───────────────────────────────────────────────────────
    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            var oldEmail = user.Email;
            var newEmail = request.Email.Trim().ToLowerInvariant();

            // Email uniqueness check (excluding self)
            var emailOwner = await _userRepository.GetByEmailAsync(newEmail, cancellationToken);
            if (emailOwner is not null && emailOwner.UserId != userId)
                throw new InvalidOperationException("A user with this email already exists.");

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
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        try { File.Delete(oldFilePath); } catch {}
                    }
                }
                user.ProfileImage = null;
            }
            else if (request.ProfileImage.StartsWith("data:image"))
            {
                // Delete old file if it exists to avoid server clutter and junk files
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    var relativePath = user.ProfileImage.Split('?')[0];
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));
                    if (File.Exists(oldFilePath))
                    {
                        try { File.Delete(oldFilePath); } catch {}
                    }
                }

                var base64Data = request.ProfileImage.Substring(request.ProfileImage.IndexOf(",") + 1);
                var imageBytes = Convert.FromBase64String(base64Data);

                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "user_images");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Detect image extension (default to png as requested, fallback if needed)
                var extension = "png";
                if (request.ProfileImage.Contains("image/jpeg") || request.ProfileImage.Contains("image/jpg"))
                {
                    extension = "jpg";
                }

                var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
                var cleanUsername = user.Username.Replace(" ", "_").ToLowerInvariant();
                var fileName = $"{dateStr}{cleanUsername}.{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
                
                // Append cache buster parameter to DB path to bypass browser caching and reflect instantly
                var cacheBuster = DateTime.UtcNow.Ticks;
                user.ProfileImage = $"/user_images/{fileName}?v={cacheBuster}";
            }

            _userRepository.Update(user);

            // ── JOIN & UPDATE CORRESPONDING MEMBER RECORD (Zero DB Alter) ─────────
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
                // If this user does not have a member record yet (e.g. system admin), create one so the profile is fully backed
                var allRoles = await _roleRepository.GetAllAsync(cancellationToken);
                var defaultRole = allRoles.FirstOrDefault(r => r.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)) 
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
                        MemberType = string.IsNullOrWhiteSpace(request.MemberType) ? "Office" : request.MemberType.Trim(),
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
                ?? throw new KeyNotFoundException("Updated user could not be loaded.");

            return await EnrichUserDtoWithMemberProfileAsync(updated, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateProfileAsync");
            throw;
        }
    }
}
