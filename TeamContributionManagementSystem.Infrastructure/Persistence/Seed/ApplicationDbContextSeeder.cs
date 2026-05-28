using Microsoft.EntityFrameworkCore;
using TeamContributionManagementSystem.Application.Interfaces.Auth;
using TeamContributionManagementSystem.Domain.Entities;
using TeamContributionManagementSystem.Domain.Enums;

namespace TeamContributionManagementSystem.Infrastructure.Persistence.Seed;

public class ApplicationDbContextSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationDbContextSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Database will be created if it does not already exist
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var adminUser = new AppUser
        {
            UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            Username = "admin",
            Email = "admin@teamcontribution.local",
            FullName = "System Administrator",
            PasswordHash = _passwordHasher.HashPassword("Admin@123"),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        await _context.Roles.AddRangeAsync(roles, cancellationToken);
        await _context.EventTypes.AddRangeAsync(eventTypes, cancellationToken);
        await _context.Users.AddAsync(adminUser, cancellationToken);

        // Seeding default Role Rights
        var defaultPages = new[]
        {
            (Module: "Dashboard", SubModule: "Analytics", Page: "Dashboard"),
            (Module: "Members", SubModule: "Directory", Page: "Members"),
            (Module: "Events", SubModule: "Registry", Page: "Events"),
            (Module: "Events", SubModule: "Calendar", Page: "Calendar"),
            (Module: "Contributions", SubModule: "Ledger", Page: "Contributions"),
            (Module: "Contributions", SubModule: "Calculation", Page: "Calculation"),
            (Module: "Support Data", SubModule: "Categories", Page: "Event Types"),
            (Module: "Support Data", SubModule: "Clearance", Page: "Exit Process"),
            (Module: "Support Data", SubModule: "Admin", Page: "User Rights"),
            (Module: "Support Data", SubModule: "Admin", Page: "Users"),
            (Module: "Reports", SubModule: "Analytics", Page: "Reports")
        };

        var roleRightsList = new List<RoleRight>();

        foreach (var role in Enum.GetValues<UserRole>())
        {
            foreach (var page in defaultPages)
            {
                string access = "readWrite"; // default for Admin / Manager

                if (role == UserRole.User || role == UserRole.Member)
                {
                    if (page.Page == "Event Types" || page.Page == "Exit Process" || page.Page == "User Rights" || page.Page == "Users")
                    {
                        access = "deny";
                    }
                    else
                    {
                        access = "readOnly";
                    }
                }

                roleRightsList.Add(new RoleRight
                {
                    RoleRightId = Guid.NewGuid(),
                    Role = role,
                    Module = page.Module,
                    SubModule = page.SubModule,
                    Page = page.Page,
                    Access = access
                });
            }
        }

        await _context.RoleRights.AddRangeAsync(roleRightsList, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
