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
        // 🚨 Temporarily deleting the database so it recreates with the correct snake_case schema
        await _context.Database.EnsureDeletedAsync(cancellationToken);
        await _context.Database.EnsureCreatedAsync(cancellationToken);

        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var roles = new[]
        {
            new Role { RoleId = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = "Intern", DefaultContributionAmount = 150 },
            new Role { RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = "Developer", DefaultContributionAmount = 300 },
            new Role { RoleId = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = "Manager", DefaultContributionAmount = 500 }
        };

        var eventTypes = new[]
        {
            new EventType { EventTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), EventTypeName = "Birthday", IsActive = true },
            new EventType { EventTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), EventTypeName = "Farewell", IsActive = true },
            new EventType { EventTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), EventTypeName = "Team Dinner", IsActive = true },
            new EventType { EventTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), EventTypeName = "Custom Event", IsActive = true }
        };

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

        var memberUser = new AppUser
        {
            UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            Username = "member",
            Email = "member@teamcontribution.local",
            FullName = "General Member",
            PasswordHash = _passwordHasher.HashPassword("Member@123"),
            Role = UserRole.Member,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        var members = new[]
        {
            new Member
            {
                MemberId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"),
                Name = "Aarav Patel",
                Email = "aarav.patel@team.local",
                Phone = "9876543210",
                RoleId = roles[1].RoleId,
                DateOfBirth = new DateTime(1996, 4, 18, 0, 0, 0, DateTimeKind.Utc),
                JoiningDate = new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Member
            {
                MemberId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"),
                Name = "Nisha Verma",
                Email = "nisha.verma@team.local",
                Phone = "9876501234",
                RoleId = roles[2].RoleId,
                DateOfBirth = new DateTime(1992, 4, 25, 0, 0, 0, DateTimeKind.Utc),
                JoiningDate = new DateTime(2021, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new Member
            {
                MemberId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"),
                Name = "Rohan Das",
                Email = "rohan.das@team.local",
                Phone = "9012345678",
                RoleId = roles[0].RoleId,
                DateOfBirth = new DateTime(1999, 6, 5, 0, 0, 0, DateTimeKind.Utc),
                JoiningDate = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        };

        var kickoffEvent = new Event
        {
            EventId = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
            EventName = "April Team Dinner",
            EventTypeId = eventTypes[2].EventTypeId,
            EventDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(10), DateTimeKind.Utc),
            CreatedBy = adminUser.UserId,
            Description = "Quarterly dinner contribution event.",
            Status = EventStatus.Planned,
            IsDeleted = false
        };

        var participants = members.Select(member => new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = kickoffEvent.EventId,
            MemberId = member.MemberId
        }).ToList();

        var contributions = members.Select(member => new Contribution
        {
            ContributionId = Guid.NewGuid(),
            EventId = kickoffEvent.EventId,
            MemberId = member.MemberId,
            Amount = roles.First(x => x.RoleId == member.RoleId).DefaultContributionAmount,
            PaymentStatus = member.MemberId == members[0].MemberId ? PaymentStatus.Paid : PaymentStatus.Pending,
            PaymentDate = member.MemberId == members[0].MemberId ? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc) : null,
            PaymentMode = member.MemberId == members[0].MemberId ? PaymentMode.Upi : PaymentMode.None,
            IsDeleted = false
        }).ToList();

        await _context.Roles.AddRangeAsync(roles, cancellationToken);
        await _context.EventTypes.AddRangeAsync(eventTypes, cancellationToken);
        await _context.Users.AddRangeAsync(new[] { adminUser, memberUser }, cancellationToken);
        await _context.Members.AddRangeAsync(members, cancellationToken);
        await _context.Events.AddAsync(kickoffEvent, cancellationToken);
        await _context.EventParticipants.AddRangeAsync(participants, cancellationToken);
        await _context.Contributions.AddRangeAsync(contributions, cancellationToken);

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
