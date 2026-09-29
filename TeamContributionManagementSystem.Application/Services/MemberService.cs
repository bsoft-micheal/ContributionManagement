using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Common;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class MemberService : IMemberService
{
    private readonly ILogger<MemberService> _logger;
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWorkTypeRepository? _workTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService? _currentUserService;

    public MemberService(
        ILogger<MemberService> logger,
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IWorkTypeRepository? workTypeRepository = null,
        ICurrentUserService? currentUserService = null)
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _workTypeRepository = workTypeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // If caller is in the Member role, restrict results to their own member record only
            if (_currentUserService != null && string.Equals(_currentUserService.Role, "Member", StringComparison.OrdinalIgnoreCase))
            {
                var userEmail = _currentUserService.Email;
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    var myMember = await _memberRepository.GetByEmailAsync(userEmail.Trim(), cancellationToken);
                    if (myMember != null)
                    {
                        var singleDto = _mapper.Map<MemberDto>(myMember);
                        return new List<MemberDto> { singleDto };
                    }
                }
                return Array.Empty<MemberDto>();
            }

            var dtos = await _memberRepository.GetAllAsync(cancellationToken);

            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);
                var userDict = users.ToDictionary(u => u.UserId.ToString(), u => u.FullName, StringComparer.OrdinalIgnoreCase);

                foreach (var dto in dtos)
                {
                    if (!string.IsNullOrWhiteSpace(dto.CreatedBy) && userDict.TryGetValue(dto.CreatedBy, out var createdByName))
                    {
                        dto.CreatedBy = createdByName;
                    }
                    if (!string.IsNullOrWhiteSpace(dto.ModifiedBy) && userDict.TryGetValue(dto.ModifiedBy, out var modByName))
                    {
                        dto.ModifiedBy = modByName;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not resolve user names for member audit fields");
            }

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetAllAsync));
            throw;
        }
    }

    public async Task<IReadOnlyCollection<MemberDto>> GetMembersWithoutUserAccountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _memberRepository.GetMembersWithoutUserAccountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(GetMembersWithoutUserAccountAsync));
            throw;
        }
    }

    public async Task<MemberDto> CreateAsync(CreateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingMember = await _memberRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (existingMember is not null)
            {
                throw new InvalidOperationException(CommonMessages.Members.EmailExists);
            }

            Guid? roleId = null;
            if (request.RoleId.HasValue && request.RoleId.Value != Guid.Empty)
            {
                var role = await _roleRepository.GetByIdAsync(request.RoleId.Value, cancellationToken);
                if (role != null) roleId = role.RoleId;
            }

            var workType = !string.IsNullOrWhiteSpace(request.WorkType)
                ? request.WorkType.Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(workType) && _workTypeRepository != null)
            {
                var dbWorkTypes = await _workTypeRepository.GetAllAsync(true, cancellationToken);
                workType = dbWorkTypes.FirstOrDefault()?.WorkTypeName ?? string.Empty;
            }

            var member = new Member
            {
                MemberId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = request.Phone.Trim(),
                RoleId = roleId,
                DateOfBirth = request.DateOfBirth.Date,
                JoiningDate = request.JoiningDate.Date,
                Gender = request.Gender,
                IsActive = request.IsActive,
                IsExited = request.IsExited,
                WorkType = workType ?? string.Empty,
                CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user.Trim()
            };

            await _memberRepository.AddAsync(member, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _memberRepository.GetByIdAsync(member.MemberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Members.NotFound);

            _logger.LogInformation(CommonLogMessages.Members.MemberCreated, member.Name, member.MemberId);
            var resultDto = _mapper.Map<MemberDto>(created);
            if (!string.IsNullOrWhiteSpace(resultDto.CreatedBy) && Guid.TryParse(resultDto.CreatedBy, out var cGuid))
            {
                var u = await _userRepository.GetByIdAsync(cGuid, cancellationToken);
                if (u != null) resultDto.CreatedBy = u.FullName;
            }
            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(CreateAsync));
            throw;
        }
    }

    public async Task<MemberDto> UpdateAsync(Guid memberId, UpdateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentUserService != null && string.Equals(_currentUserService.Role, "Member", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Members are not authorized to modify member records here.");
            }

            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Members.NotFound);

            var duplicate = await _memberRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.MemberId != memberId)
            {
                throw new InvalidOperationException(CommonMessages.Members.EmailExists);
            }

            if (request.RoleId.HasValue && request.RoleId.Value != Guid.Empty)
            {
                var role = await _roleRepository.GetByIdAsync(request.RoleId.Value, cancellationToken);
                if (role != null) member.RoleId = role.RoleId;
            }

            member.Name = request.Name.Trim();
            member.Email = request.Email.Trim().ToLowerInvariant();
            member.Phone = request.Phone.Trim();
            member.DateOfBirth = request.DateOfBirth.Date;
            member.JoiningDate = request.JoiningDate.Date;
            member.Gender = request.Gender;
            member.IsActive = request.IsActive;
            member.IsExited = request.IsExited;
            var updatedWorkType = !string.IsNullOrWhiteSpace(request.WorkType) ? request.WorkType.Trim() : null;
            if (!string.IsNullOrWhiteSpace(updatedWorkType))
            {
                member.WorkType = updatedWorkType;
            }
            if (!string.IsNullOrWhiteSpace(user))
            {
                member.ModifiedBy = user.Trim();
            }

            _memberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updated = await _memberRepository.GetByIdAsync(member.MemberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Members.NotFound);

            _logger.LogInformation(CommonLogMessages.Members.MemberUpdated, member.MemberId);
            var resultDto = _mapper.Map<MemberDto>(updated);
            if (!string.IsNullOrWhiteSpace(resultDto.CreatedBy) && Guid.TryParse(resultDto.CreatedBy, out var cGuid))
            {
                var u = await _userRepository.GetByIdAsync(cGuid, cancellationToken);
                if (u != null) resultDto.CreatedBy = u.FullName;
            }
            if (!string.IsNullOrWhiteSpace(resultDto.ModifiedBy) && Guid.TryParse(resultDto.ModifiedBy, out var mGuid))
            {
                var u = await _userRepository.GetByIdAsync(mGuid, cancellationToken);
                if (u != null) resultDto.ModifiedBy = u.FullName;
            }
            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(UpdateAsync));
            throw;
        }
    }

    public async Task DeleteAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentUserService != null && string.Equals(_currentUserService.Role, "Member", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Members are not authorized to delete member records.");
            }

            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Members.NotFound);

            member.IsDeleted = true;
            member.IsActive = false;

            _memberRepository.Update(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(CommonLogMessages.Members.MemberDeleted, memberId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, CommonLogMessages.General.ErrorInMethod, nameof(DeleteAsync));
            throw;
        }
    }
}
