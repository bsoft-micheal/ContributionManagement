using Microsoft.Extensions.Logging;
using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Domain.Entities;

namespace TeamContributionManagementSystem.Application.Services;

public class MemberService : IMemberService
{
    private readonly Microsoft.Extensions.Logging.ILogger<MemberService> _logger;
    private readonly IMemberRepository _memberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MemberService(Microsoft.Extensions.Logging.ILogger<MemberService> logger, IMemberRepository memberRepository, IRoleRepository roleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _logger = logger;
        _memberRepository = memberRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<MemberDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _memberRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyCollection<MemberDto>>(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllAsync");
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
            throw new InvalidOperationException("A member with the same email already exists.");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");

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
            MemberType = string.IsNullOrWhiteSpace(request.MemberType) ? "Office" : request.MemberType,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? null : user,
            CreatedAt = DateTime.UtcNow
        };

        await _memberRepository.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _memberRepository.GetByIdAsync(member.MemberId, cancellationToken)
            ?? throw new KeyNotFoundException("Created member could not be loaded.");

        return _mapper.Map<MemberDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateAsync");
            throw;
        }
    }

    public async Task<MemberDto> UpdateAsync(Guid memberId, UpdateMemberRequestDto request, string? user = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new KeyNotFoundException("Member not found.");

        var duplicate = await _memberRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
    if (duplicate is not null && duplicate.MemberId != memberId)
        {
            throw new InvalidOperationException("A member with the same email already exists.");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");

        member.Name = request.Name.Trim();
        member.Email = request.Email.Trim().ToLowerInvariant();
        member.Phone = request.Phone.Trim();
        member.RoleId = role.RoleId;
        member.DateOfBirth = request.DateOfBirth.Date;
        member.JoiningDate = request.JoiningDate.Date;
        member.Gender = request.Gender;
        member.IsActive = request.IsActive;
        member.IsExited = request.IsExited;
        member.MemberType = string.IsNullOrWhiteSpace(request.MemberType) ? "Office" : request.MemberType;
        member.ModifiedBy = string.IsNullOrWhiteSpace(user) ? null : user;
        member.ModifiedOn = DateTime.UtcNow;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _memberRepository.GetByIdAsync(member.MemberId, cancellationToken)
            ?? throw new KeyNotFoundException("Updated member could not be loaded.");

        return _mapper.Map<MemberDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAsync");
            throw;
        }
    }

    public async Task DeleteAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken)
            ?? throw new KeyNotFoundException("Member not found.");

        member.IsDeleted = true;
        member.IsActive = false;

        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteAsync");
            throw;
        }
    }
}
