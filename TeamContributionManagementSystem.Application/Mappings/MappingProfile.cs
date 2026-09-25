using AutoMapper;
using TeamContributionManagementSystem.Application.DTOs.BudgetCalculations;
using TeamContributionManagementSystem.Application.DTOs.Contributions;
using TeamContributionManagementSystem.Application.DTOs.Events;
using TeamContributionManagementSystem.Application.DTOs.EventTypes;
using TeamContributionManagementSystem.Application.DTOs.Expenses;
using TeamContributionManagementSystem.Application.DTOs.Gallery;
using TeamContributionManagementSystem.Application.DTOs.Members;
using TeamContributionManagementSystem.Application.DTOs.Payments;
using TeamContributionManagementSystem.Application.DTOs.Roles;
using TeamContributionManagementSystem.Application.DTOs.Settings;
using TeamContributionManagementSystem.Application.DTOs.SupportTickets;
using TeamContributionManagementSystem.Application.DTOs.Statuses;
using TeamContributionManagementSystem.Application.DTOs.TicketTypes;
using TeamContributionManagementSystem.Application.DTOs.WorkTypes;
using TeamContributionManagementSystem.Application.DTOs.Users;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt ?? src.CreatedOn))
            .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn));

        CreateMap<Member, MemberDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : string.Empty))
            .ForMember(dest => dest.DefaultContributionAmount, opt => opt.MapFrom(src => src.Role != null ? src.Role.DefaultContributionAmount : 0))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt ?? src.CreatedOn))
            .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.ModifiedOn));

        CreateMap<AppUser, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt ?? src.CreatedOn));

        CreateMap<EventType, EventTypeDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<EventParticipant, EventParticipantDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? src.Member.Name : string.Empty))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Member != null && src.Member.Role != null ? src.Member.Role.RoleName : string.Empty));

        CreateMap<Contribution, ContributionDto>()
            .ForMember(dest => dest.EventName, opt => opt.MapFrom(src => src.Event != null ? src.Event.EventName : string.Empty))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? src.Member.Name : string.Empty))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Event != null && src.Event.EventType != null ? src.Event.EventType.EventTypeName : string.Empty))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<Event, EventSummaryDto>()
            .ForMember(dest => dest.EventTypeName, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.EventTypeName : string.Empty))
            .ForMember(dest => dest.HasTenureRule, opt => opt.MapFrom(src => src.EventType != null && src.EventType.HasTenureRule))
            .ForMember(dest => dest.TenureThresholdYears, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.TenureThresholdYears : 1.0m))
            .ForMember(dest => dest.NewEntrantSharePercentage, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.NewEntrantSharePercentage : 50.0m))
            .ForMember(dest => dest.StandardSharePercentage, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.StandardSharePercentage : 100.0m))
            .ForMember(dest => dest.RuleDescription, opt => opt.MapFrom(src => src.EventType != null ? src.EventType.RuleDescription : null))
            .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
            .ForMember(dest => dest.TotalExpectedAmount, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted).Sum(x => x.Amount)))
            .ForMember(dest => dest.TotalPaidAmount, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted && x.PaymentStatus == PaymentStatus.Paid).Sum(x => x.Amount)))
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : (src.CreatedBy != Guid.Empty ? src.CreatedBy.ToString() : null)))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : (src.CreatedBy != Guid.Empty ? src.CreatedBy.ToString() : null)))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<Event, EventDetailsDto>()
            .IncludeBase<Event, EventSummaryDto>()
            .ForMember(dest => dest.Contributions, opt => opt.MapFrom(src => src.Contributions.Where(x => !x.IsDeleted)));

        CreateMap<RoleRight, RoleRightDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.AccessType, opt => opt.MapFrom(src => (int)src.AccessType))
            .ReverseMap()
            .ForMember(dest => dest.Role, opt => opt.Ignore()); // Role set explicitly in service


        CreateMap<Expense, ExpenseDto>()
            .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<SupportTicket, SupportTicketDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<SystemSetting, SettingItemDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<PaymentTransaction, PaymentTransactionDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<GalleryPhoto, GalleryPhotoDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<BudgetCalculation, BudgetCalculationDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<TicketType, TicketTypeDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<Status, StatusDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));

        CreateMap<WorkType, WorkTypeDto>()
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.CreatedAt));
    }
}
