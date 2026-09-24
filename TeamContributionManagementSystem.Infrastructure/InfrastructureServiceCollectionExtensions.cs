using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Application.Interfaces.Repositories;
using TeamContributionManagementSystem.Application.Interfaces.Services;
using TeamContributionManagementSystem.Application.Mappings;
using TeamContributionManagementSystem.Application.Services;
using TeamContributionManagementSystem.Infrastructure.Persistence;
using TeamContributionManagementSystem.Infrastructure.Persistence.Seed;
using TeamContributionManagementSystem.Infrastructure.Repositories;
using TeamContributionManagementSystem.Infrastructure.Services;

namespace TeamContributionManagementSystem.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAndInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConnString")));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IEventTypeRepository, EventTypeRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IContributionRepository, ContributionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRightRepository, RoleRightRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IGalleryRepository, GalleryRepository>();
        services.AddScoped<IDeviceSessionRepository, DeviceSessionRepository>();
        services.AddScoped<IUserMfaDeviceRepository, UserMfaDeviceRepository>();
        services.AddScoped<IBudgetCalculationRepository, BudgetCalculationRepository>();

        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IEventTypeService, EventTypeService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IContributionService, ContributionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBirthdayAutomationService, BirthdayAutomationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleRightsService, RoleRightsService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IPaymentTransactionService, PaymentTransactionService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IBudgetCalculationService, BudgetCalculationService>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ApplicationDbContextSeeder>();

        services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

        return services;
    }
}
