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
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public UserManagementService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork     = unitOfWork;
        _passwordHasher = passwordHasher;
        _mapper         = mapper;
    }

    // ── GET ALL ──────────────────────────────────────────────────────────────
    public async Task<IReadOnlyCollection<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<UserDto>>(users);
    }

    // ── CREATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
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

        return _mapper.Map<UserDto>(created);
    }

    // ── UPDATE ───────────────────────────────────────────────────────────────
    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
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

        user.Username = request.Username.Trim();
        user.Email    = request.Email.Trim().ToLowerInvariant();
        user.Role     = role;
        user.IsActive = request.IsActive;

        // Update password only when a new one is supplied
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _userRepository.GetByIdAsync(user.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Updated user could not be loaded.");

        return _mapper.Map<UserDto>(updated);
    }

    // ── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        _userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
