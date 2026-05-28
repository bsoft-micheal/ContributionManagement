using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Role, RoleDto>();

        CreateMap<Member, MemberDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty))
            .ForMember(dest => dest.DefaultContributionAmount, opt => opt.MapFrom(src => src.Role != null ? src.Role.DefaultContributionAmount : 0));

        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<EventType, EventTypeDto>();

        CreateMap<EventParticipant, EventParticipantDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? src.Member.Name : string.Empty))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Member != null && src.Member.Role != null ? src.Member.Role.RoleName : string.Empty));

        CreateMap<Contribution, ContributionDto>()
            .ForMember(dest => dest.EventName, opt => opt.MapFrom(src => src.Event != null ? src.Event.EventName : string.Empty))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? src.Member.Name : string.Empty));

        CreateMap<Event, EventSummaryDto>()
            .ForMember(dest => dest.EventTypeName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.EventTypeName : string.Empty))
            .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
            .ForMember(dest => dest.TotalExpectedAmount, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted).Sum(x => x.Amount)))
            .ForMember(dest => dest.TotalPaidAmount, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted && x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount)));

        CreateMap<Event, EventDetailsDto>()
            .IncludeBase<Event, EventSummaryDto>()
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : string.Empty))
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
            .ForMember(dest => dest.Contributions, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted)));

        CreateMap<RoleRight, RoleRightDto>().ReverseMap();
    }
}
