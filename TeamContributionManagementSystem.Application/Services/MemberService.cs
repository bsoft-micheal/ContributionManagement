using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.Common;
using TeamContributionManagementSystem.Application.DTOs.Members;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MemberService(
        ILogger<MemberService> logger,
        IMemberRepository memberRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _memberRepository.GetAllAsync(cancellationToken);
            var dtos = _mapper.Map<List<MemberDto>>(members);

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

    public async Task<MemberDto> CreateAsync(CreateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingMember = await _memberRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (existingMember is not null)
            {
                throw new InvalidOperationException(CommonMessages.Members.EmailExists);
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Roles.NotFound);

            var member = new Member
            {
                MemberId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = request.Phone.Trim(),
                RoleId = role.RoleId,
                DateOfBirth = request.DateOfBirth.Date,
                JoiningDate = request.JoiningDate.Date,
                Gender = request.Gender,
                IsActive = request.IsActive,
                IsExited = request.IsExited,
                MemberType = request.MemberType?.Trim() ?? string.Empty,
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
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Members.NotFound);

            var duplicate = await _memberRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (duplicate is not null && duplicate.MemberId != memberId)
            {
                throw new InvalidOperationException(CommonMessages.Members.EmailExists);
            }

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException(CommonMessages.Roles.NotFound);

            member.Name = request.Name.Trim();
            member.Email = request.Email.Trim().ToLowerInvariant();
            member.Phone = request.Phone.Trim();
            member.RoleId = role.RoleId;
            member.DateOfBirth = request.DateOfBirth.Date;
            member.JoiningDate = request.JoiningDate.Date;
            member.Gender = request.Gender;
            member.IsActive = request.IsActive;
            member.IsExited = request.IsExited;
            member.MemberType = !string.IsNullOrWhiteSpace(request.MemberType) ? request.MemberType.Trim() : member.MemberType;
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
